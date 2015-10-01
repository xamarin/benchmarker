# Wrench Poller
from twisted.python import log
from twisted.internet import defer, reactor, utils
from twisted.web.client import Agent, getPage

from buildbot import config
from buildbot.changes import base
from buildbot.util import epoch2datetime
from buildbot.util.state import StateMixin
from buildbot.process.buildstep import LoggingBuildStep
from buildbot.status.builder import SUCCESS, FAILURE

from constants import BOSTON_NAS_URL, PROPERTYNAME_BOSTONNAS_PKGURL

import itertools
import json
import re
import os
import urllib


# based on upstream GitPoller
class BostonNasPoller(base.PollingChangeSource, StateMixin):
    compare_attrs = ["repourl", "branches", "wrenchlane", "workdir", "pollInterval", "gitbin", "usetimestamps", "category", "project", "pollAtLaunch"]

    def __init__(self, repourl, branches=None, branch=None, wrenchlane=None, workdir=None, pollInterval=20 * 60, gitbin='git', usetimestamps=True,
                 category=None, project=None, fetch_refspec=None, encoding='utf-8', pollAtLaunch=False):

        base.PollingChangeSource.__init__(self, name=repourl, pollInterval=pollInterval, pollAtLaunch=pollAtLaunch)
        if project is None:
            project = ''

        if branch and branches:
            config.error("BostonNasPoller: can't specify both branch and branches")
        elif branch:
            branches = [branch]
        elif not branches:
            branches = ['master']

        if wrenchlane is None:
            config.error("BostonNasPoller: need to specify wrenchlane")

        self.repourl = repourl
        self.branches = branches
        self.wrenchlane = wrenchlane
        self.encoding = encoding
        self.gitbin = gitbin
        self.workdir = workdir
        self.usetimestamps = usetimestamps
        self.category = category
        self.project = project
        self.knownRevs = []
        self.dbName = 'knownRevsASDF'

        if fetch_refspec is not None:
            config.error("BostonNasPoller: fetch_refspec is no longer supported. "
                         "Instead, only the given branches are downloaded.")

        if self.workdir is None:
            self.workdir = 'BostonNasPoller-%s-work' % self.wrenchlane

    def startService(self):
        # make our workdir absolute, relative to the master's basedir
        if not os.path.isabs(self.workdir):
            self.workdir = os.path.join(self.master.basedir, self.workdir)
            log.msg("BostonNasPoller: using workdir '%s'" % self.workdir)

        d = self.getState(self.dbName, [])

        def setKnownRevs(knownRevs):
            self.knownRevs = knownRevs
        d.addCallback(setKnownRevs)
        d.addCallback(lambda _: base.PollingChangeSource.startService(self))
        d.addErrback(log.err, 'while initializing BostonNasPoller repository')

        return d

    def describe(self):
        str = ('BostonNasPoller watching the remote git repository ' + self.repourl)

        if self.branches:
            if self.branches is True:
                str += ', branches: ALL'
            elif not callable(self.branches):
                str += ', branches: ' + ', '.join(self.branches)

        if not self.master:
            str += " [STOPPED - check log]"

        return str

    def _getBranches(self):
        d = self._dovccmd('ls-remote', [self.repourl])

        @d.addCallback
        def parseRemote(rows):
            branches = []
            for row in rows.splitlines():
                if '\t' not in row:
                    # Not a useful line
                    continue
                sha, ref = row.split("\t")
                branches.append(ref)
            return branches
        return d

    def _headsFilter(self, branch):
        """Filter out remote references that don't begin with 'refs/heads'."""
        return branch.startswith("refs/heads/")

    def _removeHeads(self, branch):
        """Remove 'refs/heads/' prefix from remote references."""
        if branch.startswith("refs/heads/"):
            branch = branch[11:]
        return branch

    def _trackerBranch(self, branch):
        return "refs/buildbot/%s/%s" % (urllib.quote(self.repourl, ''), self._removeHeads(branch))

    @defer.inlineCallbacks
    def poll(self):
        yield self._dovccmd('init', ['--bare', self.workdir])

        branches = self.branches
        if branches is True or callable(branches):
            branches = yield self._getBranches()
            if callable(self.branches):
                branches = filter(self.branches, branches)
            else:
                branches = filter(self._headsFilter, branches)

        refspecs = ['+%s:%s' % (self._removeHeads(branch), self._trackerBranch(branch)) for branch in branches]
        yield self._dovccmd('fetch', [self.repourl] + refspecs, path=self.workdir)

        for branch in branches:
            try:
                yield self._process_changes(branch)
            except Exception:
                log.err(_why="trying to poll branch %s of %s" % (branch, self.repourl))

        yield self.setState(self.dbName, self.knownRevs)

    def _decode(self, git_output):
        return git_output.decode(self.encoding)

    def _get_commit_comments(self, rev):
        args = ['--no-walk', r'--format=%s%n%b', rev, '--']
        d = self._dovccmd('log', args, path=self.workdir)
        d.addCallback(self._decode)
        return d

    def _get_commit_timestamp(self, rev):
        # unix timestamp
        args = ['--no-walk', r'--format=%ct', rev, '--']
        d = self._dovccmd('log', args, path=self.workdir)

        def process(git_output):
            if self.usetimestamps:
                try:
                    stamp = float(git_output)
                except Exception, e:
                    log.msg('BostonNasPoller: caught exception converting output \'%s\' to timestamp' % git_output)
                    raise e
                return stamp
            else:
                return None
        d.addCallback(process)
        return d

    def _get_commit_files(self, rev):
        args = ['--name-only', '--no-walk', r'--format=%n', rev, '--']
        d = self._dovccmd('log', args, path=self.workdir)

        def decode_file(file):
            # git use octal char sequences in quotes when non ASCII
            match = re.match('^"(.*)"$', file)
            if match:
                file = match.groups()[0].decode('string_escape')
            return self._decode(file)

        def process(git_output):
            fileList = [decode_file(file) for file in itertools.ifilter(lambda s: len(s), git_output.splitlines())]
            return fileList
        d.addCallback(process)
        return d

    def _get_commit_author(self, rev):
        args = ['--no-walk', r'--format=%aN <%aE>', rev, '--']
        d = self._dovccmd('log', args, path=self.workdir)

        def process(git_output):
            git_output = self._decode(git_output)
            if len(git_output) == 0:
                raise EnvironmentError('could not get commit author for rev')
            return git_output
        d.addCallback(process)
        return d
    
    def _construct_boston_url(self, rev):
        return '%s/%s/%s/%s/' % (BOSTON_NAS_URL, self.wrenchlane, rev[:2], rev)

    @defer.inlineCallbacks
    def _process_changes(self, branch):
        """
        check if builds are available.
        - Add changes to database.
        """

        # get the change list
        revListArgs = [r'--format=%H', r'--since', r'8 weeks ago', self._trackerBranch(branch)]
        results = yield self._dovccmd('log', revListArgs, path=self.workdir)
        revList = results.split()

        log.msg('BostonNasPoller: processing %d changes: %s from "%s"' % (len(revList), revList, self.repourl))

        for rev in revList:
            if rev in self.knownRevs:
                continue

            pageExists = yield _exists_d(self._construct_boston_url(rev) + '/manifest')
            if not pageExists:
                continue
            
            self.knownRevs.append(rev)

            dl = defer.DeferredList([self._get_commit_timestamp(rev), self._get_commit_author(rev), self._get_commit_files(rev), self._get_commit_comments(rev)], consumeErrors=True)
            results = yield dl

            # check for failures
            failures = [r[1] for r in results if not r[0]]
            if failures:
                # just fail on the first error; they're probably all related!
                raise failures[0]

            timestamp, author, files, comments = [r[1] for r in results]
            yield self.master.addChange(
                author=author, revision=rev, files=files, comments=comments, when_timestamp=epoch2datetime(timestamp),
                branch=self._removeHeads(branch), category=self.category, project=self.project, repository=self.repourl, src='git'
            )

    def _dovccmd(self, command, args, path=None):
        d = utils.getProcessOutputAndValue(self.gitbin, [command] + args, path=path, env=os.environ)

        def _convert_nonzero_to_failure(res, command, args, path):
            "utility to handle the result of getProcessOutputAndValue"
            (stdout, stderr, code) = res
            if code != 0:
                raise EnvironmentError('command %s %s in %s on repourl %s failed with exit code %d: %s'
                                       % (command, args, path, self.repourl, code, stderr))
            return stdout.strip()
        d.addCallback(_convert_nonzero_to_failure, command, args, path)
        return d


def _mk_request_manifest(base_url, wrenchlane, rev, logger):
    url = "%s/%s/%s/%s/metadata.json" % (base_url, wrenchlane, rev[:2], rev)
    url = url.encode('ascii', 'ignore')
    if logger:
        logger("request: " + str(url))
    return getPage(url)

@defer.inlineCallbacks
def _fetch_pkg_name(base_url, wrenchlane, rev, prefix, suffix, logger):
    j = json.loads((yield _mk_request_manifest(base_url, wrenchlane, rev, logger)))
    for filename in j.keys():
        if filename.startswith(prefix) and filename.endswith(suffix):
            defer.returnValue("%s/%s/%s/%s/%s" % (base_url, wrenchlane, rev[:2], rev, filename))
    assert False, "storage blew up?"
    defer.returnValue(None)

def _exists_d(url):
    def handleResponse(r):
        return r.code in (200, 301, 302)

    def handleError(reason):
        reason.printTraceback()

    d = Agent(reactor).request('POST', url)
    d.addCallbacks(handleResponse, handleError)
    return d

class BostonNasGetPackageUrlStep(LoggingBuildStep):
    def __init__(self, base_url, wrenchlane, prefix, suffix, *args, **kwargs):
        self.base_url = base_url
        self.wrenchlane = wrenchlane
        self.prefix = prefix
        self.suffix = suffix
        LoggingBuildStep.__init__(self, name="BostonNasGetPackageUrlStep", haltOnFailure=True, *args, **kwargs)

    def start(self):
        #pylint: disable=E1101
        stdoutlogger = self.addLog("stdio").addStdout
        #pylint: enable=E1101
        self.logger = lambda msg: stdoutlogger(msg + "\n")
        dfrd = self._do_request()
        dfrd.addCallbacks(self._finished_ok, self._finished_failure)

    @defer.inlineCallbacks
    def _do_request(self):
        rev = self.getProperty('revision')
        url = yield _fetch_pkg_name(self.base_url, self.wrenchlane, rev, self.prefix, self.suffix, self.logger)
        self.setProperty(PROPERTYNAME_BOSTONNAS_PKGURL, url)

    def _finished_ok(self, res):
        log.msg("BostonNasGetPackageUrlStep success")
        self.finished(SUCCESS)
        return res

    def _finished_failure(self, res):
        errmsg = "BostonNasGetPackageUrlStep failed: %s" % str(res)
        log.msg(errmsg)
        self.logger(errmsg)
        self.finished(FAILURE)
        return None

# for testing/debugging
if __name__ == '__main__':
    def stop_me():
        #pylint: disable=E1101
        if reactor.running:
            reactor.stop()
            print "stopping..."
        #pylint: enable=E1101

    def _logger(msg):
        print msg

    @defer.inlineCallbacks
    def test_check_boston_nas():
        url= r'http://storage.bos.internalx.com/mono-master-monodroid/13/1354dc1044387ad7b2919db1ba08db510e61a8db/mono-android-5.0.99-209.pkg'
        e = yield _exists_d(url)
        print "does it exists? " + str(e)
        pkg = yield _fetch_pkg_name(BOSTON_NAS_URL, 'mono-master-monodroid', '1354dc1044387ad7b2919db1ba08db510e61a8db', 'mono-android', '.pkg', _logger)
        print "download: " + pkg

    @defer.inlineCallbacks
    def run_tests():
        _ = yield test_check_boston_nas()
        stop_me()

    run_tests()
    #pylint: disable=E1101
    reactor.run()
    #pylint: enable=E1101


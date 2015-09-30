# Jenkins Poller
from twisted.python import log
from twisted.internet import defer, reactor
from twisted.web.client import getPage

from buildbot.status.builder import SUCCESS, FAILURE

from buildbot.process.buildstep import BuildStep, LoggingBuildStep
from buildbot.changes import base
from buildbot.util import epoch2datetime

from HTMLParser import HTMLParser
import operator
import requests
import json
from constants import MONO_BASEURL, MONO_PULLREQUEST_BASEURL, MONO_COMMON_SNAPSHOTS_URL, MONO_SOURCETARBALL_URL, MONO_SOURCETARBALL_PULLREQUEST_URL, PROPERTYNAME_JENKINSBUILDURL, PROPERTYNAME_JENKINSGITCOMMIT, PROPERTYNAME_JENKINSGITHUBPULLREQUEST, FORCE_PROPERTYNAME_JENKINS_BUILD, Lane
import re

import credentials


import itertools
import os
import re
import urllib

from twisted.internet import defer
from twisted.internet import utils
from twisted.python import log

from buildbot import config
from buildbot.util.state import StateMixin


# based on upstream GitPoller
class BostonNASPoller(base.PollingChangeSource, StateMixin):
    compare_attrs = ["repourl", "branches", "workdir", "pollInterval", "gitbin", "usetimestamps", "category", "project", "pollAtLaunch"]

    def __init__(self, repourl, branches=None, branch=None, workdir=None, pollInterval=10 * 60, gitbin='git', usetimestamps=True,
                 category=None, project=None, fetch_refspec=None, encoding='utf-8', pollAtLaunch=False):

        base.PollingChangeSource.__init__(self, name=repourl, pollInterval=pollInterval, pollAtLaunch=pollAtLaunch)
        if project is None:
            project = ''

        if branch and branches:
            config.error("bostonnaspoller: can't specify both branch and branches")
        elif branch:
            branches = [branch]
        elif not branches:
            branches = ['master']

        self.repourl = repourl
        self.branches = branches
        self.encoding = encoding
        self.gitbin = gitbin
        self.workdir = workdir
        self.usetimestamps = usetimestamps
        self.category = category
        self.project = project
        self.changeCount = 0
        self.lastRev = {}

        if fetch_refspec is not None:
            config.error("bostonnaspoller: fetch_refspec is no longer supported. "
                         "Instead, only the given branches are downloaded.")

        if self.workdir is None:
            self.workdir = 'bostonnaspoller-work'

    def startService(self):
        # make our workdir absolute, relative to the master's basedir
        if not os.path.isabs(self.workdir):
            self.workdir = os.path.join(self.master.basedir, self.workdir)
            log.msg("bostonnaspoller: using workdir '%s'" % self.workdir)

        d = self.getState('lastRev', {})

        def setLastRev(lastRev):
            self.lastRev = lastRev
        d.addCallback(setLastRev)
        d.addCallback(lambda _: base.PollingChangeSource.startService(self))
        d.addErrback(log.err, 'while initializing bostonnaspoller repository')

        return d

    def describe(self):
        str = ('bostonnaspoller watching the remote git repository ' + self.repourl)

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

        revs = {}
        for branch in branches:
            try:
                rev = yield self._dovccmd('rev-parse', [self._trackerBranch(branch)], path=self.workdir)
                revs[branch] = str(rev)
                yield self._process_changes(revs[branch], branch)
            except Exception:
                log.err(_why="trying to poll branch %s of %s" % (branch, self.repourl))

        self.lastRev.update(revs)
        yield self.setState('lastRev', self.lastRev)

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
                    log.msg('bostonnaspoller: caught exception converting output \'%s\' to timestamp' % git_output)
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

    @defer.inlineCallbacks
    def _process_changes(self, newRev, branch):
        """
        Read changes since last change.

        - Read list of commit hashes.
        - Extract details from each commit.
        - Add changes to database.
        """

        # initial run, don't parse all history
        if not self.lastRev:
            return
        if newRev in self.lastRev.values():
            # TODO: no new changes on this branch
            # should we just use the lastRev again, but with a different branch?
            pass

        # get the change list
        revListArgs = ([r'--format=%H', r'%s' % newRev] + [r'^%s' % rev for rev in self.lastRev.values()] + [r'--'])
        self.changeCount = 0
        results = yield self._dovccmd('log', revListArgs, path=self.workdir)

        # process oldest change first
        revList = results.split()
        revList.reverse()
        self.changeCount = len(revList)
        self.lastRev[branch] = newRev

        log.msg('bostonnaspoller: processing %d changes: %s from "%s"' % (self.changeCount, revList, self.repourl))

        for rev in revList:
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


def _mk_request_wrench_build_matrix(base_url, lane, logger):
    url = (base_url + '/Wrench/index.aspx?lane=' + lane).encode('ascii', 'ignore')
    if logger:
        logger("request: " + str(url))
    params = credentials.getWrenchHeader()
    return getPage(url, headers=params)

def _mk_request_wrench_single_build(url, logger):
    url = url.encode('ascii', 'ignore')
    if logger:
        logger("request: " + str(url))
    params = credentials.getWrenchHeader()
    return getPage(url, headers=params)

def _exists(url):
    response = requests.head(url)
    print response.status_code, response.text, response.headers
    return response.status_code in (200, 301, 302)


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

    def test_commits_monodroid():


    def test_check_boston_nas():
        url= r'http://storage.bos.internalx.com/mono-master-monodroid/13/1354dc1044387ad7b2919db1ba08db510e61a8db/mono-android-5.0.99-209.pkg'
        print "does it exists? " + str(_exists(url))

    @defer.inlineCallbacks
    def run_tests():
        _ = yield getPage('http://google.com')
        _ = test_check_boston_nas()
        stop_me()

    run_tests()
    #pylint: disable=E1101
    reactor.run()
    #pylint: enable=E1101


# Jenkins Poller
from twisted.python import log
from twisted.internet import defer, reactor
from twisted.web.client import getPage

from buildbot.status.builder import SUCCESS, FAILURE

from buildbot.process.buildstep import BuildStep, LoggingBuildStep
from buildbot.changes import base
from buildbot.util import epoch2datetime

#pylint: disable=F0401
import credentials
#pylint: enable=F0401
import json
from constants import MONO_BASEURL, MONO_PULLREQUEST_BASEURL, MONO_COMMON_SNAPSHOTS_URL, MONO_SOURCETARBALL_URL, MONO_SOURCETARBALL_PULLREQUEST_URL, PROPERTYNAME_JENKINSBUILDURL, PROPERTYNAME_JENKINSGITCOMMIT, PROPERTYNAME_JENKINSGITHUBPULLREQUEST, FORCE_PROPERTYNAME_JENKINS_BUILD, Lane
import re
import urllib

def gen_jenkinspoller_codebase(lane, platform, hostname, config_name):
    return 'mono-jenkins-%s%s-%s-%s' % ("-pullrequest" if lane == Lane.PullRequest else "", platform, hostname, config_name)

class MonoJenkinsPoller(base.PollingChangeSource):
    compare_attrs = ['lane', 'url', 'platform', 'hostname', 'config_name', 'fake_repo_url']
    parent = None
    working = False

    def __init__(self, url, fake_repo_url, lane, platform, hostname, config_name, *args, **kwargs):
        self.url = url
        self.fake_repo_url = fake_repo_url
        self.lane = lane
        self.platform = platform
        self.hostname = hostname
        self.config_name = config_name
        self.db_class_name = "MonoJenkinsPoller-%s-%s-%s-%s" % (str(lane), platform, hostname, config_name)

        base.PollingChangeSource.__init__(self, pollAtLaunch=False, *args, **kwargs)

        self.branch = None

    def describe(self):
        return "Getting changes from MonoJenkins %s/%s" % (str(self.url), str(self.db_class_name))

    def poll(self):
        if self.working:
            log.msg("Not polling because last poll is still working")
        else:
            self.working = True
            dfrd = _get_new_jenkins_changes(self.url, self.platform, self.hostname, self.config_name)
            dfrd.addCallback(self._process_changes)
            dfrd.addCallback(self._finished_ok)
            dfrd.addErrback(self._finished_failure)
            return dfrd

    def _finished_ok(self, res):
        log.msg("MonoJenkinsPoller(%s) poll success: %s" % (self.db_class_name, str(res)))
        assert self.working
        self.working = False

    def _finished_failure(self, res):
        log.msg("MonoJenkinsPoller(%s) poll failed: %s" % (self.db_class_name, str(res)))
        assert self.working
        self.working = False

    def _get_state_object_id(self):
        """Return a deferred for object id in state db.
        """
        return self.master.db.state.getObjectId(
            '#'.join((self.platform, self.hostname, self.config_name)), self.db_class_name)

    def _get_current_rev(self):
        """Return a deferred for object id in state db and current numeric rev.

        If never has been set, current rev is None.
        """
        dfrd = self._get_state_object_id()

        def oid_cb(oid):
            dfrd = self.master.db.state.getState(oid, 'current_rev', None)

            def add_oid(cur):
                if cur is not None:
                    return oid, int(cur)
                return oid, cur
            dfrd.addCallback(add_oid)
            return dfrd
        dfrd.addCallback(oid_cb)
        return dfrd

    def _set_current_rev(self, rev, oid=None):
        """Return a deferred to set current revision in persistent state.

        oid is self's id for state db. It can be passed to avoid a db lookup."""

        if oid is None:
            dfrd = self._get_state_object_id()
        else:
            dfrd = defer.succeed(oid)

        def set_in_state(obj_id):
            return self.master.db.state.setState(obj_id, 'current_rev', rev)
        dfrd.addCallback(set_in_state)

        return dfrd

    @defer.inlineCallbacks
    def _process_changes(self, builds_todo):
        not_added = []
        for build in builds_todo:
            oid, last_rev = yield self._get_current_rev()
            revision = build[:-1].split('/')[-1] if build[-1] == '/' else build.split('/')[-1]
            current_rev = int(revision)
            if current_rev <= int(last_rev if last_rev is not None else 0):
                not_added.append('#' + str(current_rev))
                continue
            build_details_json = json.loads((yield _mk_request_jenkins_build(build, None)))
            yield self.master.addChange(
                author='Jenkins', # TODO: get author name responsible for triggering the jenkins build
                revision=revision,
                files=[],
                comments="",
                revlink=build_details_json['url'],
                when_timestamp=epoch2datetime(int(build_details_json['timestamp']) / 1000),
                branch=None,
                category=None,
                codebase=gen_jenkinspoller_codebase(self.lane, self.platform, self.hostname, self.config_name),
                project='',
                repository=self.fake_repo_url,
                src=u'jenkins'
            )
            yield self._set_current_rev(str(current_rev), oid)
        if not_added:
            log.msg('not added to %s: %s' % (str(self.db_class_name), str(not_added)))


def _mk_request_jenkins_all_builds(base_url, platform, logger):
    url = '%s/label=%s/api/json?pretty=true&tree=allBuilds[fingerprint[original[*]],artifacts[*],url,building,result]' % (base_url, platform)
    if logger:
        logger("request: " + str(url))
    return getPage(url)

def _mk_request_jenkins_build(build_url, logger):
    url = (build_url + "/api/json?pretty=true").encode('ascii', 'ignore')
    if logger:
        logger("request: " + str(url))
    return getPage(url)

def _mk_request_jenkins_build_s3(build_url, logger):
    url = (build_url + "/s3").encode('ascii', 'ignore')
    if logger:
        logger("request: " + str(url))
    return getPage(url)

def _mk_request_mono_common(logger):
    url = MONO_COMMON_SNAPSHOTS_URL
    if logger:
        logger("request: " + str(url))
    return getPage(url)

def _mk_request_parse(hostname, config_name, logger):
    headers = credentials.getParseHeaders()
    #pylint: disable=C0330
    query = ('where={' +
                '"buildURL":{"$exists":true},' +
                '"machine":{' +
                    '"$inQuery":{' +
                        '"where":{' +
                            '"name":"' + hostname + '"' +
                        '},' +
                        '"className":"Machine"' +
                    '}' +
                '},' +
                '"config":{' +
                    '"$inQuery":{' +
                        '"where":{' +
                            '"name":"' + config_name+ '"' +
                        '},' +
                        '"className":"Config"' +
                    '}' +
                '}' +
            '}')
    #pylint: enable=C0330
    params = urllib.quote(query, '=')
    url = 'https://api.parse.com/1/classes/RunSet?%s' % params
    if logger:
        logger("request: " + str(url))
    return getPage(url, headers=headers)

@defer.inlineCallbacks
def _get_new_jenkins_changes(jenkins_base_url, platform, hostname, config_name):
    jenkins_json = json.loads((yield _mk_request_jenkins_all_builds(jenkins_base_url, platform, None)))
    parse_json = json.loads((yield _mk_request_parse(hostname, config_name, None)))

    def _filter_build_urls_jenkins(j):
        return sorted([i['url'].encode('ascii', 'ignore') for i in j['allBuilds'] if i['result'] == 'SUCCESS'])

    def _filter_build_urls_parse(j):
        return sorted([i['buildURL'].encode('ascii', 'ignore') for i in j['results'] if i.has_key('buildURL') and i['buildURL'] is not None])

    builds_todo = list(set(_filter_build_urls_jenkins(jenkins_json)) - set(_filter_build_urls_parse(parse_json)))
    defer.returnValue(sorted(builds_todo))


def debug_pp_json(j):
    print json.dumps(j, sort_keys=True, indent=4, separators=(',', ': '))


class BuildURLToPropertyStep(LoggingBuildStep):
    def __init__(self, base_url, *args, **kwargs):
        self.base_url = base_url
        LoggingBuildStep.__init__(self, name="buildURL2prop", haltOnFailure=True, *args, **kwargs)

    def start(self):
        # label=debian-amd64/1348/
        platform = self.getProperty('platform')
        jenkins_source_base = None
        for i in self.build.sources:
            if 'jenkins' in i.repository:
                assert jenkins_source_base is None
                jenkins_source_base = i

        build_nr = None
        if jenkins_source_base is None:
            build_nr = self.getProperty(FORCE_PROPERTYNAME_JENKINS_BUILD)
        else:
            build_nr = jenkins_source_base.revision
        assert build_nr is not None, "no jenkins source base found: " + reduce(lambda x, y: str(x.repository) + ', ' + str(y.repository), self.build.sources)
        self.setProperty(PROPERTYNAME_JENKINSBUILDURL, self.base_url + '/label=' + platform + '/' + build_nr + '/')
        self.finished(SUCCESS)


class FetchJenkinsBuildDetails(BuildStep):
    def __init__(self, base_url, sourcetarball_url, *args, **kwargs):
        self.logger = None
        self.base_url = base_url
        self.sourcetarball_url = sourcetarball_url
        BuildStep.__init__(self, haltOnFailure=True, flunkOnFailure=True, *args, **kwargs)

    def start(self):
        #pylint: disable=E1101
        stdoutlogger = self.addLog("stdio").addStdout
        #pylint: enable=E1101
        self.logger = lambda msg: stdoutlogger(msg + "\n")
        dfrd = self._do_request()
        dfrd.addCallbacks(self._finished_ok, self._finished_failure)

    def _finished_ok(self, res):
        log.msg("FetchJenkinsBuildDetails success")
        self.finished(SUCCESS)
        return res

    def _finished_failure(self, res):
        errmsg = "FetchJenkinsBuildDetails failed: %s" % str(res)
        log.msg(errmsg)
        self.logger(errmsg)
        self.finished(FAILURE)
        return None

    @defer.inlineCallbacks
    def _do_request(self):
        build_url = self.getProperty(PROPERTYNAME_JENKINSBUILDURL)
        platform = self.getProperty('platform')
        assert build_url is not None, "property should be there! :-("
        log.msg("before fetching meta data")
        urls = yield _do_fetch_build(build_url, platform, self.logger)
        git_commit = self.getProperty(PROPERTYNAME_JENKINSGITCOMMIT)
        if git_commit is None or git_commit == "":
            gitrev = yield _do_fetch_gitrev(build_url, self.base_url, self.sourcetarball_url, platform, self.logger)
            for key, value in gitrev.items():
                urls[key] = value
        for prop_name, filename in urls.items():
            log.msg('adding: ' + str((prop_name, filename)))
            self.setProperty(prop_name, filename)


from HTMLParser import HTMLParser
class HTMLParserMonoCommon(HTMLParser):
    def __init__(self, *args, **kwargs):
        self.common_deb = None
        HTMLParser.__init__(self, *args, **kwargs)

    def handle_starttag(self, tag, attrs):
        if tag == 'a':
            for name, value in attrs:
                if name == 'href' and value.endswith('_all.deb'):
                    self.common_deb = value

class HTMLParserS3Artifacts(HTMLParser):
    def __init__(self, result, build_url, get_path, *args, **kwargs):
        self.result = result
        self.build_url = build_url
        self.get_path = get_path
        HTMLParser.__init__(self, *args, **kwargs)

    def handle_starttag(self, tag, attrs):
        if tag != 'a':
            return

        for name, path in attrs:
            if name != 'href':
                continue
            if not path.startswith('download'):
                continue
            if '.changes' in path or '-latest' in path:
                continue
            self.get_path(self, path)

@defer.inlineCallbacks
def _do_fetch_build(build_url, platform, logger):
    result = {}

    parsermono = HTMLParserMonoCommon()
    parsermono.feed((yield _mk_request_mono_common(logger)))
    assert parsermono.common_deb is not None, 'no common debian package found :-('
    result['deb_common_url'] = MONO_COMMON_SNAPSHOTS_URL + '/' + parsermono.common_deb

    # get assemblies package always from debian-amd64 builder.
    amd64build_url = build_url.replace(platform, 'debian-amd64')
    def _get_assemblies(parser, path):
        if '-assemblies' in path:
            parser.result['deb_asm_url'] = parser.build_url + '/s3/' + path
    parserassembly = HTMLParserS3Artifacts(result, amd64build_url, _get_assemblies)
    parserassembly.feed((yield _mk_request_jenkins_build_s3(amd64build_url, logger)))

    # get bin package from arch specific builder
    def _get_bin(parser, path):
        if '-assemblies' not in path:
            parser.result['deb_bin_url'] = parser.build_url + '/s3/' + path
    parserassembly = HTMLParserS3Artifacts(result, build_url, _get_bin)
    parserassembly.feed((yield _mk_request_jenkins_build_s3(build_url, logger)))

    assert len(result) == 3, 'should contain three elements, but contains: ' + str(result)
    defer.returnValue(result)


@defer.inlineCallbacks
def _do_fetch_gitrev(build_url, base_url, sourcetarball_url, platform, logger):
    result = {}

    # get git revision from jenkins
    json_all = json.loads((yield _mk_request_jenkins_all_builds(base_url, platform, logger)))
    gitrev = None
    for i in [i for i in json_all['allBuilds'] if i['url'].encode('ascii', 'ignore') == build_url]:
        for fingerprint in i['fingerprint']:
            fp_name = fingerprint['original']['name']
            if fp_name == 'build-source-tarball-mono':
                assert gitrev is None, "should set gitrev only once"
                url = sourcetarball_url + str(fingerprint['original']['number']) + '/pollingLog/pollingLog'
                if logger:
                    logger("request: " + str(url))
                regexgitrev = re.compile("Latest remote head revision on [a-zA-Z/]+ is: (?P<gitrev>[0-9a-fA-F]+)")
                match = regexgitrev.search((yield getPage(url)))
                assert match is not None
                gitrev = match.group('gitrev')
            if fp_name == 'build-source-tarball-mono-pullrequest':
                assert gitrev is None, "should set gitrev only once"
                url = sourcetarball_url + str(fingerprint['original']['number']) + '/consoleText'
                if logger:
                    logger("request: " + str(url))
                regexgitrev = re.compile("GitHub pull request #(?P<prid>[0-9]+) of commit (?P<gitrev>[0-9a-fA-F]+)")
                match = regexgitrev.search((yield getPage(url)))
                assert match is not None
                gitrev = match.group('gitrev')
                result[PROPERTYNAME_JENKINSGITHUBPULLREQUEST] = match.group('prid')
    assert gitrev is not None, "parsing gitrev failed"
    result[PROPERTYNAME_JENKINSGITCOMMIT] = gitrev
    defer.returnValue(result)

# for testing/debugging
if __name__ == '__main__':
    def stop_me():
        #pylint: disable=E1101
        if reactor.running:
            reactor.stop()
            print "stopping..."
        #pylint: enable=E1101

    @defer.inlineCallbacks
    def test_fetch_build_debarm():
        def _logger(msg):
            print msg

        results = yield _do_fetch_build('https://jenkins.mono-project.com/view/All/job/build-package-dpkg-mono/label=debian-armhf/1403/', 'debian-armhf', _logger)
        for key, value in results.items():
            print "%s: %s" % (key, value)

    @defer.inlineCallbacks
    def test_get_changes_debarm():
        print ""
        print "URLs to process:"
        for url in sorted((yield _get_new_jenkins_changes(MONO_BASEURL, 'debian-armhf', 'utilite-desktop', 'auto-sgen'))):
            print url


    @defer.inlineCallbacks
    def test_fetch_build_debamd64():
        def _logger(msg):
            print msg

        build_url = 'https://jenkins.mono-project.com/view/All/job/build-package-dpkg-mono/label=debian-amd64/1541/'
        results = yield _do_fetch_build(build_url, 'debian-amd64', _logger)
        results.update((yield _do_fetch_gitrev(build_url, MONO_BASEURL, MONO_SOURCETARBALL_URL, 'debian-amd64', _logger)))
        for key, value in results.items():
            print "%s: %s" % (key, value)

    @defer.inlineCallbacks
    def test_get_changes_debamd64():
        print ""
        print "URLs to process:"
        for url in sorted((yield _get_new_jenkins_changes(MONO_BASEURL, 'debian-amd64', 'bernhard-vbox-linux', 'auto-sgen-noturbo'))):
            print url


    @defer.inlineCallbacks
    def test_fetch_pr_build_debamd64():
        def _logger(msg):
            print msg

        build_url = 'https://jenkins.mono-project.com/view/All/job/build-package-dpkg-mono-pullrequest/label=debian-amd64/4/'
        results = yield _do_fetch_build(build_url, 'debian-amd64', _logger)
        results.update((yield _do_fetch_gitrev(build_url, MONO_PULLREQUEST_BASEURL, MONO_SOURCETARBALL_PULLREQUEST_URL, 'debian-amd64', _logger)))
        for key, value in results.items():
            print "%s: %s" % (key, value)

    @defer.inlineCallbacks
    def test_get_pr_changes_debamd64():
        print ""
        print "URLs to process:"
        for url in sorted((yield _get_new_jenkins_changes(MONO_PULLREQUEST_BASEURL, 'debian-amd64', 'bernhard-vbox-linux', 'auto-sgen-noturbo'))):
            print url


    @defer.inlineCallbacks
    def run_tests():
        # _ = yield test_get_changes_debarm()
        # _ = yield test_fetch_build_debarm()
        _ = yield test_get_changes_debamd64()
        _ = yield test_fetch_build_debamd64()
        # _ = yield test_get_pr_changes_debamd64()
        _ = yield test_fetch_pr_build_debamd64()
        stop_me()

    run_tests()
    #pylint: disable=E1101
    reactor.run()
    #pylint: enable=E1101


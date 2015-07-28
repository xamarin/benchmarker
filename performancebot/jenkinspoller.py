# Jenkins Poller
from twisted.python import log
from twisted.internet import defer, reactor
from twisted.web.client import getPage

from buildbot.status.builder import SUCCESS, FAILURE

from buildbot.process.buildstep import BuildStep, LoggingBuildStep
from buildbot.changes import base
from buildbot.util import epoch2datetime

import credentials
import json
import re
import urllib

monoBaseUrl = 'https://jenkins.mono-project.com/view/All/job/build-package-dpkg-mono'
monoCommonSnapshotsUrl = 'http://jenkins.mono-project.com/repo/debian/pool/main/m/mono-snapshot-common/'
monoSourceTarballUrl = 'https://jenkins.mono-project.com/job/build-source-tarball-mono/'

class MonoJenkinsPoller(base.PollingChangeSource):
    compare_attrs = ['url', 'platform', 'hostname', 'config_name', 'pollInterval', 'fakeRepoUrl']
    parent = None
    working = False

    def __init__(self, url, fakeRepoUrl, platform, hostname, config_name, pollInterval=300):
        self.url = url
        self.fakeRepoUrl = fakeRepoUrl
        self.platform = platform
        self.hostname = hostname
        self.config_name = config_name
        self.db_class_name = "MonoJenkinsPoller-%s-%s-%s" % (platform, hostname, config_name)


        base.PollingChangeSource.__init__(self, pollInterval = pollInterval, pollAtLaunch = True)

        self.branch = None

    def describe(self):
        return ("Getting changes from MonoJenkins %s" % str(self.url))

    def poll(self):
        if self.working:
            log.msg("Not polling because last poll is still working")
        else:
            self.working = True
            d = _getNewJenkinChanges(self.url, self.platform, self.hostname, self.config_name)
            d.addCallback(self._process_changes)
            d.addCallbacks(self._finished_ok, self._finished_failure)
            return d

    def _finished_ok(self, res):
        assert self.working
        self.working = False
        log.msg("MonoJenkinsPoller poll success")

        return res

    def _finished_failure(self, res):
        log.msg("MonoJenkinsPoller poll failed: %s" % res)
        assert self.working
        self.working = False
        return None

    def _getStateObjectId(self):
        """Return a deferred for object id in state db.
        """
        return self.master.db.state.getObjectId(
            '#'.join((self.platform, self.hostname, self.config_name)), self.db_class_name)

    def _getCurrentRev(self):
        """Return a deferred for object id in state db and current numeric rev.

        If never has been set, current rev is None.
        """
        d = self._getStateObjectId()

        def oid_cb(oid):
            d = self.master.db.state.getState(oid, 'current_rev', None)

            def addOid(cur):
                if cur is not None:
                    return oid, int(cur)
                return oid, cur
            d.addCallback(addOid)
            return d
        d.addCallback(oid_cb)
        return d

    def _setCurrentRev(self, rev, oid=None):
        """Return a deferred to set current revision in persistent state.

        oid is self's id for state db. It can be passed to avoid a db lookup."""

        if oid is None:
            d = self._getStateObjectId()
        else:
            d = defer.succeed(oid)

        def set_in_state(obj_id):
            return self.master.db.state.setState(obj_id, 'current_rev', rev)
        d.addCallback(set_in_state)

        return d

    @defer.inlineCallbacks
    def _process_changes(self, change_list):
        for change in change_list:
            oid, lastRev = yield self._getCurrentRev()
            if int(change['revision']) <= int(lastRev if lastRev is not None else 0):
                log.msg('not adding: #' + str(change['revision']) + ' (last one: #' + str(lastRev) + ')')
                continue
            log.msg('adding change:' + str(change))
            yield self.master.addChange(
                    author = change['who'],
                    revision = change['revision'],
                    files = [],
                    comments = "",
                    revlink = change['url'],
                    when_timestamp = epoch2datetime(int(change['when']) / 1000),
                    branch = None,
                    category = None,
                    codebase = change['codebase'],
                    project = '',
                    repository = self.fakeRepoUrl,
                    src=u'jenkins')
            yield self._setCurrentRev(change['revision'], oid)
        if change_list:
            self.lastChange = change_list[-1]["revision"]




def _mkRequestJenkinsAllBuilds(baseUrl, platform, logger):
    url = '%s/label=%s/api/json?pretty=true&tree=allBuilds[fingerprint[original[*]],artifacts[*],url,building,result]' % (baseUrl, platform)
    if logger:
        logger("request: " + str(url))
    return getPage(url)

def _mkRequestJenkinsSingleBuild(buildUrl, logger):
    url = (buildUrl + "/api/json?pretty=true").encode('ascii', 'ignore')
    if logger:
        logger("request: " + str(url))
    return getPage(url)

def _mkRequestJenkinsSingleBuildS3(buildUrl, logger):
    url = (buildUrl + "/s3").encode('ascii', 'ignore')
    if logger:
        logger("request: " + str(url))
    return getPage(url)

def _mkRequestMonoCommonSnapshot(logger):
    url = monoCommonSnapshotsUrl
    if logger:
        logger("request: " + str(url))
    return getPage(url)

def _mkRequestParse(hostname, config_name, logger):
    headers = credentials.getParseHeaders()
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
    params = urllib.quote(query, '=')
    url = 'https://api.parse.com/1/classes/RunSet?%s' % params
    if logger:
        logger("request: " + str(url))
    return getPage(url, headers = headers)

@defer.inlineCallbacks
def _getNewJenkinChanges(jenkinsBaseUrl, platform, hostname, config_name):
    jenkinsRequest = yield _mkRequestJenkinsAllBuilds(monoBaseUrl, platform, None)
    jenkinsJson = json.loads(jenkinsRequest)
    parseRequest = yield _mkRequestParse(hostname, config_name, None)
    parseJson = json.loads(parseRequest)

    def _filterBuildURLsJenkins(j):
        return sorted([i['url'].encode('ascii', 'ignore') for i in j['allBuilds'] if i['result'] == 'SUCCESS'])

    def _filterBuildURLsParse(j):
        return sorted([i['buildURL'].encode('ascii', 'ignore') for i in j['results'] if i.has_key('buildURL') and i['buildURL'] is not None])

    buildsToDo = list(set(_filterBuildURLsJenkins(jenkinsJson)) - set(_filterBuildURLsParse(parseJson)))

    result = []
    for b in buildsToDo:
        buildDetailsRequest = yield _mkRequestJenkinsSingleBuild(b, None)
        buildDetailsJson = json.loads(buildDetailsRequest)
        result.append({
            'who': 'Jenkins', # TODO: get author name responsible for triggering the jenkins build
            'revision': buildDetailsJson['number'],
            'url': buildDetailsJson['url'],
            'codebase': 'mono-jenkins-%s-%s-%s' % (platform, hostname, config_name),
            'when': buildDetailsJson['timestamp']
        })

    defer.returnValue(sorted(result, key = lambda k: k['revision']))



def ppJson(j):
    print(json.dumps(j, sort_keys=True, indent=4, separators=(',', ': ')))


PROPERTYNAME_JENKINSBUILDURL = 'jenkins-buildURL'
PROPERTYNAME_JENKINSGITCOMMIT = 'jenkins-gitCommit'

class BuildURLToPropertyStep(LoggingBuildStep):
    def __init__(self, baseUrl, *args, **kwargs):
        self.baseUrl = baseUrl
        LoggingBuildStep.__init__(self, name = "buildURL2prop", haltOnFailure = True, *args, **kwargs)

    def start(self):
        # label=debian-amd64/1348/
        platform = self.getProperty('platform')
        jenkinsSourceBase = None
        for i in self.build.sources:
            if 'jenkins' in i.repository:
                assert jenkinsSourceBase is None
                jenkinsSourceBase = i

        assert jenkinsSourceBase is not None, "no jenkins source base found: " + reduce(lambda x,y: str(x.repository) + ', ' + str(y.repository), self.build.sources)
        buildNr = jenkinsSourceBase.revision
        self.setProperty(PROPERTYNAME_JENKINSBUILDURL, self.baseUrl + '/label=' + platform + '/' + buildNr + '/')
        self.finished(SUCCESS)


class FetchJenkinsBuildDetails(BuildStep):
    def __init__(self, *args, **kwargs):
        BuildStep.__init__(self, haltOnFailure = True, flunkOnFailure = True, *args, **kwargs)

    def start(self):
        stdoutlogger = self.addLog("stdio").addStdout
        self.logger = lambda msg: stdoutlogger(msg + "\n")
        d = self.doRequest()
        d.addCallbacks(self._finished_ok, self._finished_failure)

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
    def doRequest(self):
        buildUrl = self.getProperty(PROPERTYNAME_JENKINSBUILDURL)
        platform = self.getProperty('platform')
        assert buildUrl is not None, "property should be there! :-("
        log.msg("before fetching meta data")
        urls = yield _doFetchBuild(buildUrl, platform, self.logger)
        for propName, filename in urls.items():
            log.msg('adding: ' + str((propName, filename)))
            self.setProperty(propName, filename)


from HTMLParser import HTMLParser
class HTMLParserMonoCommon(HTMLParser):
    def __init__(self, *args, **kwargs):
        self.commonDeb = None
        HTMLParser.__init__(self, *args, **kwargs)

    def handle_starttag(self, tag, attrs):
        if tag == 'a':
            for name, value in attrs:
                if name == 'href' and value.endswith('_all.deb'):
                    self.commonDeb = value

class HTMLParserS3Artifacts(HTMLParser):
    def __init__(self, result, buildUrl, getPath, *args, **kwargs):
        self.result = result
        self.buildUrl = buildUrl
        self.getPath = getPath
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
            self.getPath(self, path)

@defer.inlineCallbacks
def _doFetchBuild(buildUrl, platform, logger):
    result = {}

    monoCommonRequest = yield _mkRequestMonoCommonSnapshot(logger)
    parsermono = HTMLParserMonoCommon()
    parsermono.feed(monoCommonRequest)
    assert parsermono.commonDeb is not None, 'no common debian package found :-('
    result['deb_common_url'] = monoCommonSnapshotsUrl + '/' + parsermono.commonDeb

    # get assemblies package always from debian-amd64 builder.
    amd64BuildUrl = buildUrl.replace(platform, 'debian-amd64')
    s3assembly = yield _mkRequestJenkinsSingleBuildS3(amd64BuildUrl, logger)
    def _getAssemblies(s, path):
        if '-assemblies' in path:
            s.result['deb_asm_url'] = s.buildUrl + '/s3/' + path
    parserassembly = HTMLParserS3Artifacts(result, amd64BuildUrl, _getAssemblies)
    parserassembly.feed(s3assembly)

    # get bin package from arch specific builder
    s3bin = yield _mkRequestJenkinsSingleBuildS3(buildUrl, logger)
    def _getBin(s, path):
        if '-assemblies' not in path:
            s.result['deb_bin_url'] = s.buildUrl + '/s3/' + path
    parserassembly = HTMLParserS3Artifacts(result, buildUrl, _getBin)
    parserassembly.feed(s3bin)

    assert len(result) == 3, 'should contain three elements, but contains: ' + str(result)

    # get git revision from jenkins
    requestAll = yield _mkRequestJenkinsAllBuilds(monoBaseUrl, platform, logger)
    jsonAll = json.loads(requestAll)
    gitrev = None
    for i in [i for i in jsonAll['allBuilds'] if i['url'].encode('ascii', 'ignore') == buildUrl]:
        for fp in i['fingerprint']:
            if fp['original']['name'] == 'build-source-tarball-mono':
                assert gitrev is None, "should set gitrev only once"
                url = monoSourceTarballUrl + str(fp['original']['number']) + '/pollingLog/pollingLog'
                if logger:
                    logger("request: " + str(url))
                pollLog = yield getPage(url)
                regexgitrev = re.compile("Latest remote head revision on [a-zA-Z/]+ is: (?P<gitrev>[0-9a-fA-F]+)")
                m = regexgitrev.search(pollLog)
                assert m is not None
                gitrev = m.group('gitrev')
    assert gitrev is not None, "parsing gitrev failed"
    result[PROPERTYNAME_JENKINSGITCOMMIT] = gitrev

    defer.returnValue(result)


# for testing/debugging
if __name__ == '__main__':
    def stopMe():
        if reactor.running:
            reactor.stop()
            print "stopping..."

    @defer.inlineCallbacks
    def testFetchSingleJobDebianARM():
        def _logger(msg):
            print msg

        results = yield _doFetchBuild('https://jenkins.mono-project.com/view/All/job/build-package-dpkg-mono/label=debian-armhf/1403/', 'debian-armhf', _logger)
        for k, v in results.items():
            print "%s: %s" % (k, v)

    @defer.inlineCallbacks
    def testGetChangesDebianARM():
        changeList = yield _getNewJenkinChanges(monoBaseUrl, 'debian-armhf', 'utilite-desktop', 'auto-sgen')

        print ""
        print "URLs to process:"
        for u in sorted(changeList):
            print u

    @defer.inlineCallbacks
    def testFetchSingleJobDebianAMD64():
        def _logger(msg):
            print msg

        results = yield _doFetchBuild('https://jenkins.mono-project.com/view/All/job/build-package-dpkg-mono/label=debian-amd64/1403/', 'debian-amd64', _logger)
        for k, v in results.items():
            print "%s: %s" % (k, v)

    @defer.inlineCallbacks
    def testGetChangesDebianAMD64():
        changeList = yield _getNewJenkinChanges(monoBaseUrl, 'debian-amd64', 'bernhard-vbox-linux', 'auto-sgen-noturbo')

        print ""
        print "URLs to process:"
        for u in sorted(changeList):
            print u

    @defer.inlineCallbacks
    def runTests():
        t2 = yield testGetChangesDebianARM()
        t1 = yield testFetchSingleJobDebianARM()
        t4 = yield testGetChangesDebianAMD64()
        t3 = yield testFetchSingleJobDebianAMD64()
        stopMe()

    runTests()
    reactor.run()


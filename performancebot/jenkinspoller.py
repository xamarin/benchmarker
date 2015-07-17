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
    compare_attrs = ['url', 'platform', 'hostname', 'config_name', 'pollInterval']
    parent = None
    working = False

    db_class_name = "MonoJenkinsPoller"

    def __init__(self, url, platform, hostname, config_name, pollInterval=300):
        self.url = url
        self.platform = platform
        self.hostname = hostname
        self.config_name = config_name

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
                    repository = self.url,
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
            'codebase': 'mono-jenkins',
            'when': buildDetailsJson['timestamp']
        })

    defer.returnValue(sorted(result, key = lambda k: k['revision']))



def ppJson(j):
    print(json.dumps(j, sort_keys=True, indent=4, separators=(',', ': ')))


propertyName_jenkinsBuildURL = 'jenkins-buildURL'
propertyName_jenkinsGitCommit = 'jenkins-gitCommit'

class BuildURLToPropertyStep(LoggingBuildStep):
    def __init__(self, *args, **kwargs):
        LoggingBuildStep.__init__(self, name = "buildURL2prop", haltOnFailure = True, *args, **kwargs)

    def start(self):
        # label=debian-amd64/1348/
        platform = self.getProperty('platform')
        assert 'jenkins' in self.build.sources[0].repository, "unexpected repositories: " + reduce(lambda x,y: str(x.repository) + ', ' + str(y.repository), self.build.sources)
        buildNr = self.build.sources[0].revision
        repository = self.build.sources[0].repository
        self.setProperty(propertyName_jenkinsBuildURL, repository + '/label=' + platform + '/' + buildNr + '/')
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
        buildUrl = self.getProperty(propertyName_jenkinsBuildURL)
        platform = self.getProperty('platform')
        assert buildUrl is not None, "property should be there! :-("
        log.msg("before fetching meta data")
        urls = yield _doFetchBuild(buildUrl, platform, self.logger)
        for propName, filename in urls.items():
            log.msg('adding: ' + str((propName, filename)))
            self.setProperty(propName, filename)


from HTMLParser import HTMLParser
class MyHTMLParser(HTMLParser):
    def __init__(self, *args, **kwargs):
        self.commonDeb = None
        HTMLParser.__init__(self, *args, **kwargs)

    def handle_starttag(self, tag, attrs):
        if tag == 'a':
            for name, value in attrs:
                if name == 'href' and value.endswith('_all.deb'):
                    self.commonDeb = value

@defer.inlineCallbacks
def _doFetchBuild(buildUrl, platform, logger):
    result = {}

    monoCommonRequest = yield _mkRequestMonoCommonSnapshot(logger)
    parser = MyHTMLParser()
    parser.feed(monoCommonRequest)
    assert parser.commonDeb is not None, 'no common debian package found :-('
    result['deb_common_url'] = monoCommonSnapshotsUrl + '/' + parser.commonDeb

    request = yield _mkRequestJenkinsSingleBuild(buildUrl, logger)
    buildJson = json.loads(request)
    artifacts = buildJson['artifacts']
    verify = 0
    for a in artifacts:
        relPath = a['relativePath']
        if '.changes' not in relPath and '-latest' not in relPath:
            if '-assemblies' in relPath:
                result['deb_asm_url'] = buildUrl + '/artifact/' + relPath
            else:
                result['deb_bin_url'] = buildUrl + '/artifact/' + relPath
            verify += 1

    assert verify == 2, 'verify is: ' + str(verify) + ', but should be 2. result: ' + str(result)

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
    result[propertyName_jenkinsGitCommit] = gitrev

    defer.returnValue(result)


# for testing/debugging
if __name__ == '__main__':
    def stopMe():
        if reactor.running:
            reactor.stop()
            print "stopping..."

    @defer.inlineCallbacks
    def testFetchSingleJob():
        results = yield _doFetchBuild('https://jenkins.mono-project.com/view/All/job/build-package-dpkg-mono/label=debian-amd64/1352/', 'debian-amd64', None)
        for k, v in results.items():
            print "%s: %s" % (k, v)

    @defer.inlineCallbacks
    def testGetChanges():
        changeList = yield _getNewJenkinChanges(monoBaseUrl, 'debian-amd64', 'bernhard-vbox-linux', 'auto-sgen-noturbo')

        print ""
        print "URLs to process:"
        for u in sorted(changeList):
            print u

    @defer.inlineCallbacks
    def runTests():
        t1 = yield testFetchSingleJob()
        t2 = yield testGetChanges()
        stopMe()

    runTests()
    reactor.run()


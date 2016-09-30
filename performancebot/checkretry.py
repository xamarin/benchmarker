from constants import BUILDBOT_URL, PROPERTYNAME_JENKINSBUILDURL, PROPERTYNAME_JENKINSGITCOMMIT, PROPERTYNAME_RUNSETID, PROPERTYNAME_SKIP_BENCHS
import json

from twisted.python import log
from twisted.web.client import getPage
from twisted.internet import defer, reactor

from buildbot.process.buildstep import LoggingBuildStep
from buildbot.status.builder import SUCCESS

class CheckRetryStep(LoggingBuildStep):
    def __init__(self, *args, **kwargs):
        LoggingBuildStep.__init__(self, name='check-retry', *args, **kwargs)

    @defer.inlineCallbacks
    def do_request(self):
        buildername = self.getProperty('buildername')
        buildernumber = self.getProperty('buildnumber')
        buildurl = self.getProperty(PROPERTYNAME_JENKINSBUILDURL)
        gitcommit = self.getProperty(PROPERTYNAME_JENKINSGITCOMMIT)
        #pylint: disable=E1101
        logger = self.addLog("stdio").addStdout
        #pylint: enable=E1101
        log.msg("check-retry: before yielding")
        run_set_id, executed_benchs = yield check_retry(BUILDBOT_URL, buildername, buildernumber, buildurl, gitcommit, lambda msg: logger(msg + "\n"))
        if run_set_id is not None and executed_benchs is not []:
            self.setProperty(PROPERTYNAME_RUNSETID, run_set_id)
            self.setProperty(PROPERTYNAME_SKIP_BENCHS, executed_benchs)
        self.finished(SUCCESS)

    def start(self):
        self.do_request()

# Ideally, the information which benchmarks to run would come out of
# the database, not the buildbot runs.  We could do this either by
# having a compare option that outputs the outstanding benchmarks for
# a given runset, or via an option that will run benchmarks only if
# they're not already in the runset.

@defer.inlineCallbacks
def check_retry(base_url, buildername, buildernumber, build_url, gitcommit, logging):
    buildernumber = int(buildernumber)
    log.msg("check-retry: start with " + str(buildernumber))
    for build_nr in range(buildernumber-1, 0, -1):
        request_url = base_url + "json/builders/" + buildername + "/builds/" + str(build_nr)
        logging("trying... " + str(request_url))
        log.msg("check-retry: requesting page for" + str(request_url))
        #pylint: disable=W0702
        try:
            response = yield getPage(request_url)
        except:
            continue
        #pylint: enable=W0702
        log.msg("check-retry: got response for " + str(request_url))
        data = json.loads(response)
        if not data['text'] == ['retry', 'exception', 'slave', 'lost']:
            continue
        match_build_url, match_gitrev = False, False
        run_set_id = None
        for prop in data['properties']:
            if prop[0] == PROPERTYNAME_JENKINSBUILDURL:
                match_build_url = str(prop[1]) == build_url
            if prop[0] == PROPERTYNAME_JENKINSGITCOMMIT:
                match_gitrev = str(prop[1]) == gitcommit
            if prop[0] == PROPERTYNAME_RUNSETID:
                run_set_id = str(prop[1])

        if not (match_build_url and match_gitrev):
            continue
        assert data['builderName'] == buildername, "buildername doesn't match"

        logging("match! -> " + str(request_url))
        executed_benchmarks = []
        for step in data['steps']:
            texts = step['text']
            if (not texts) or (not str(texts[0]).startswith('benchmark ')) or (all([e in texts for e in ['exception', 'slave', 'lost']])):
                continue
            executed_benchmarks.append(str(step['name']))
        if executed_benchmarks is not []:
            defer.returnValue((run_set_id, executed_benchmarks))
    defer.returnValue((None, []))


# for testing/debugging
if __name__ == '__main__':
    def _print(msg):
        print msg

    @defer.inlineCallbacks
    def run_tests():
        res = yield check_retry(
            BUILDBOT_URL,
            'debian-amd64_benchmarker_auto-sgen-noturbo',
            '77',
            'https://jenkins.mono-project.com/view/All/job/build-package-dpkg-mono/label=debian-amd64/1446/',
            '98dff8f62d46acd6d159290ebfa284e4e10976d2',
            _print
        )
        print res

        #pylint: disable=E1101
        if reactor.running:
            reactor.stop()
            print "stopping..."
        #pylint: enable=E1101

    run_tests()
    #pylint: disable=E1101
    reactor.run()
    #pylint: enable=E1101


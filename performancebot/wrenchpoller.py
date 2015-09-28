# Jenkins Poller
from twisted.python import log
from twisted.internet import defer, reactor
from twisted.web.client import getPage

from buildbot.status.builder import SUCCESS, FAILURE

from buildbot.process.buildstep import BuildStep, LoggingBuildStep
from buildbot.changes import base
from buildbot.util import epoch2datetime

import operator
import json
from constants import MONO_BASEURL, MONO_PULLREQUEST_BASEURL, MONO_COMMON_SNAPSHOTS_URL, MONO_SOURCETARBALL_URL, MONO_SOURCETARBALL_PULLREQUEST_URL, PROPERTYNAME_JENKINSBUILDURL, PROPERTYNAME_JENKINSGITCOMMIT, PROPERTYNAME_JENKINSGITHUBPULLREQUEST, FORCE_PROPERTYNAME_JENKINS_BUILD, Lane
import re

import credentials

# https://wrench.internalx.com/Wrench/index.aspx?lane=mono-master-monodroid
WRENCH_INTERNAL_BASE_URL = r'https://wrench.internalx.com'

def _mk_request_wrench(base_url, lane, logger):
    url = (base_url + '/Wrench/index.aspx?lane=' + lane).encode('ascii', 'ignore')
    if logger:
        logger("request: " + str(url))
    params = credentials.getWrenchHeader()
    return getPage(url, headers=params)

# accept 'success' and 'issues' as class

from HTMLParser import HTMLParser
class HTMLParserWrenchBuildMatrix(HTMLParser):
    def __init__(self, *args, **kwargs):
        self.common_deb = None
        self.state = 0
        self.successful_builds = {}
        HTMLParser.__init__(self, *args, **kwargs)

    def handle_starttag(self, tag, attrs):
        if self.state == 0 and tag == 'table':
            for name, value in attrs:
                if name != 'class':
                    continue
                if value == 'buildstatus':
                    self.state = 1
        elif self.state == 1 and tag == 'tr':
            self.state = 2
        elif self.state == 2 and tag == 'td':
            for name, value in attrs:
                if name != 'class':
                    continue
                if value == 'issues' or value == 'success':
                    self.state = 3
        elif self.state == 3 and tag == 'a':
            for name, value in attrs:
                if name != 'href':
                    continue
                self.nextUrl = value
            self.state = 4

    def handle_endtag(self, tag):
        if self.state == 1 and tag == 'table':
            self.state = 0
        elif self.state == 2 and tag == 'tr':
            self.state = 1
        elif self.state == 3 and tag == 'td':
            self.state = 2

    def handle_data(self, data):
        if self.state == 4:
            self.successful_builds[data] = self.nextUrl
            self.state = 1


# for testing/debugging
if __name__ == '__main__':
    def stop_me():
        #pylint: disable=E1101
        if reactor.running:
            reactor.stop()
            print "stopping..."
        #pylint: enable=E1101

    @defer.inlineCallbacks
    def test_mono_master_monodroid():
        def _logger(msg):
            print msg

        parserbuildmatrix = HTMLParserWrenchBuildMatrix()
        parserbuildmatrix.feed((yield _mk_request_wrench(WRENCH_INTERNAL_BASE_URL, 'mono-master-monodroid', _logger)))
        sorted_list = sorted(parserbuildmatrix.successful_builds.items(), key=operator.itemgetter(1))
        for revision, url in sorted_list:
            print "{0}: {1}".format(revision, WRENCH_INTERNAL_BASE_URL + "/Wrench/" + url)

    @defer.inlineCallbacks
    def run_tests():
        _ = yield test_mono_master_monodroid()
        stop_me()

    run_tests()
    #pylint: disable=E1101
    reactor.run()
    #pylint: enable=E1101


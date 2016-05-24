BENCHMARKER_BRANCH = 'master'

PROPERTYNAME_MONOVERSION = 'monoversion'
PROPERTYNAME_RUNSETID = 'parse_runsetid'
PROPERTYNAME_RUNIDS = 'parse_runids'
PROPERTYNAME_PULLREQUESTID = 'parse_pullrequestid'
PROPERTYNAME_COMPARE_JSON = 'compare_json_result'

PROPERTYNAME_SKIP_BENCHS = 'skip_benchs'
PROPERTYNAME_FILTER_BENCHS = 'filter_benchs'

PROPERTYNAME_JENKINSBUILDURL = 'jenkins-buildURL'
PROPERTYNAME_JENKINSGITCOMMIT = 'jenkins-gitCommit'
PROPERTYNAME_JENKINSGITHUBPULLREQUEST = 'jenkins-pullrequestid'

PROPERTYNAME_BOSTONNAS_PKGURL = 'bostonnas-package-url'

PROPERTYNAME_BINARY_PROTOCOL_FILES = 'binary-protocol-files'

FORCE_PROPERTYNAME_JENKINS_BUILD = 'force-jenkins-build'

JENKINS_URL = 'https://jenkins.mono-project.com'

MONO_SGEN_GREP_BINPROT_GITREV = 'de3aa63805bcd12d4b18ffad636cf6f1c1f041b1'
MONO_SGEN_GREP_BINPROT_FILENAME = 'sgen-grep-binprot-%s' % MONO_SGEN_GREP_BINPROT_GITREV

MONO_BASEURL = JENKINS_URL + '/view/All/job/build-package-dpkg-mono'
MONO_COMMON_SNAPSHOTS_URL = JENKINS_URL + '/repo/debian/pool/main/m/mono-snapshot-common/'
MONO_SOURCETARBALL_URL = JENKINS_URL + '/job/build-source-tarball-mono/'

MONO_PULLREQUEST_BASEURL = JENKINS_URL + '/view/All/job/build-package-dpkg-mono-pullrequest'
MONO_SOURCETARBALL_PULLREQUEST_URL = JENKINS_URL + '/job/build-source-tarball-mono-pullrequest/'

MASTERWORKDIR = 'tmp/%(prop:buildername)s/%(prop:buildnumber)s'

BUILDBOT_URL = 'https://performancebot.mono-project.com/'

BOSTON_NAS_URL = r'http://storage.bos.internalx.com'


class Lane(object):
    Master, PullRequest = range(2)

class BenchmarkKind(object):
    Default = ""
    Massif = "valgrind_" # should be 'valgrind-massif', but we would lose logs if we rename the builder... *sigh*
    Cachegrind = "valgrind-cachegrind_"
    CompileTime = "compile-time_"


def _lifo_queue(_, requests):
    return requests[-1]

def _fifo_queue(_, requests):
    return requests[0]

class QueueType(object):
    Lifo = "lifo"
    Fifo = "fifo"

def select_queue_type(queue_type):
    if queue_type == QueueType.Lifo:
        return _lifo_queue
    if queue_type == QueueType.Fifo:
        return _fifo_queue
    assert False, "shouldn't reach"


BENCHMARKER_BRANCH = 'master'

PROPERTYNAME_MONOVERSION = 'monoversion'
PROPERTYNAME_RUNSETID = 'parse_runsetid'
PROPERTYNAME_PULLREQUESTID = 'parse_pullrequestid'
PROPERTYNAME_COMPARE_JSON = 'compare_json_result'

PROPERTYNAME_SKIP_BENCHS = 'skip_benchs'
PROPERTYNAME_FILTER_BENCHS = 'filter_benchs'

PROPERTYNAME_JENKINSBUILDURL = 'jenkins-buildURL'
PROPERTYNAME_JENKINSGITCOMMIT = 'jenkins-gitCommit'
PROPERTYNAME_JENKINSGITHUBPULLREQUEST = 'jenkins-pullrequestid'

PROPERTYNAME_BOSTONNAS_PKGURL = 'bostonnas-package-url'

FORCE_PROPERTYNAME_JENKINS_BUILD = 'force-jenkins-build'

JENKINS_URL = 'https://jenkins.mono-project.com'

MONO_SGEN_GREP_BINPROT_GITREV = '8c1d0d4efe388e49da213261f8a990cde0b62c5d'
MONO_SGEN_GREP_BINPROT_FILENAME = 'sgen-grep-binprot-%s' % MONO_SGEN_GREP_BINPROT_GITREV

MONO_BASEURL = JENKINS_URL + '/view/All/job/build-package-dpkg-mono'
MONO_COMMON_SNAPSHOTS_URL = JENKINS_URL + '/repo/debian/pool/main/m/mono-snapshot-common/'
MONO_SOURCETARBALL_URL = JENKINS_URL + '/job/build-source-tarball-mono/'

MONO_PULLREQUEST_BASEURL = JENKINS_URL + '/view/All/job/build-package-dpkg-mono-pullrequest'
MONO_SOURCETARBALL_PULLREQUEST_URL = JENKINS_URL + '/job/build-source-tarball-mono-pullrequest/'

BUILDBOT_URL = 'https://performancebot.mono-project.com'

BOSTON_NAS_URL = r'http://storage.bos.internalx.com'


class Lane(object):
    Master, PullRequest = range(2)


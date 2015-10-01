PROPERTYNAME_MONOVERSION = 'monoversion'
PROPERTYNAME_RUNSETID = 'parse_runsetid'
PROPERTYNAME_PULLREQUESTID = 'parse_pullrequestid'

PROPERTYNAME_SKIP_BENCHS = 'skip_benchs'
PROPERTYNAME_FILTER_BENCHS = 'filter_benchs'

PROPERTYNAME_JENKINSBUILDURL = 'jenkins-buildURL'
PROPERTYNAME_JENKINSGITCOMMIT = 'jenkins-gitCommit'
PROPERTYNAME_JENKINSGITHUBPULLREQUEST = 'jenkins-pullrequestid'

PROPERTYNAME_BOSTONNAS_PKGURL = 'bostonnas-package-url'

FORCE_PROPERTYNAME_JENKINS_BUILD = 'force-jenkins-build'

JENKINS_URL = 'https://jenkins.mono-project.com'

MONO_BASEURL = JENKINS_URL + '/view/All/job/build-package-dpkg-mono'
MONO_COMMON_SNAPSHOTS_URL = JENKINS_URL + '/repo/debian/pool/main/m/mono-snapshot-common/'
MONO_SOURCETARBALL_URL = JENKINS_URL + '/job/build-source-tarball-mono/'

MONO_PULLREQUEST_BASEURL = JENKINS_URL + '/view/All/job/build-package-dpkg-mono-pullrequest'
MONO_SOURCETARBALL_PULLREQUEST_URL = JENKINS_URL + '/job/build-source-tarball-mono-pullrequest/'

BUILDBOT_URL = 'https://performancebot.mono-project.com'

BOSTON_NAS_URL = r'http://storage.bos.internalx.com'


class Lane(object):
    Master, PullRequest = range(2)


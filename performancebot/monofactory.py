from buildbot import interfaces
from buildbot.process.factory import BuildFactory
from buildbot.process.properties import Interpolate
from buildbot.steps.shell import ShellCommand
from buildbot.steps.master import MasterShellCommand
from buildbot.steps.transfer import FileDownload
from buildbot.steps.source import git
from buildbot.status.builder import SUCCESS

from constants import BUILDBOT_URL, PROPERTYNAME_RUNSETID, PROPERTYNAME_PULLREQUESTID, PROPERTYNAME_SKIP_BENCHS, PROPERTYNAME_FILTER_BENCHS, PROPERTYNAME_JENKINSGITHUBPULLREQUEST
from monosteps import CreateRunSetIdStep, GithubWritePullrequestComment

import re

MASTERWORKDIR = 'tmp/%(prop:buildername)s/%(prop:buildnumber)s'


class ExpandingStep(object):
    def __init__(self, closure):
        self.closure = closure

    #pylint: disable=C0103
    def buildStep(self):
        return self
    #pylint: enable=C0103


class DebianMonoBuildFactory(BuildFactory):
    def __init__(self, *args, **kwargs):
        BuildFactory.__init__(self, *args, **kwargs)

    def add_expanding_step(self, closure):
        self.steps.append(ExpandingStep(closure))

    def newBuild(self, requests):
        """Create a new Build instance.

        @param requests: a list of buildrequest dictionaries describing what is
        to be built
        """
        build_class = self.buildClass(requests)
        build_class.useProgress = self.useProgress
        build_class.workdir = self.workdir

        generated_steps = []
        for step in self.steps:
            if isinstance(step, ExpandingStep):
                for gstep in step.closure(self):
                    generated_steps.append(interfaces.IBuildStepFactory(gstep))
            else:
                generated_steps.append(step)
        build_class.setStepFactories(generated_steps)
        return build_class

    def benchmarker_on_master(self):
        step = MasterShellCommand(
            name="build_benchmarker",
            command=[
                'bash', '-x', '-c',
                Interpolate(
                    'pwd && ' +
                    'mkdir -p %s && ' % MASTERWORKDIR +
                    'cd %s && ' % MASTERWORKDIR +
                    'git clone --depth 1 -b master https://github.com/xamarin/benchmarker && ' +
                    'cd benchmarker/tools && (/usr/bin/cli --version || true) && ' +
                    'bash ../performancebot/utils/nugethack.sh && ' +
                    'xbuild /t:compare && ' +
                    'cd ../.. && tar cvfz benchmarker.tar.gz benchmarker/tools/{*.dll,*.exe} && (md5 benchmarker.tar.gz || md5sum benchmarker.tar.gz)'
                )
                ]
        )
        self.addStep(step)

    def cleanup_master_workdir(self):
        step = MasterShellCommand(
            name="cleanup_master_workdir",
            command=['bash', '-x', '-c', Interpolate('rm -rf %s' % MASTERWORKDIR)],
            alwaysRun=True
        )
        self.addStep(step)

    def export_benchmark_list(self, machine):
        step = MasterShellCommand(
            name="list_benchmarks",
            command=[
                'bash', '-c', Interpolate(
                    'mono %s/benchmarker/tools/compare.exe --list-benchmarks --machine %s | ' % (MASTERWORKDIR, machine) +
                    'tee benchmarks-%s.list' % machine)
            ]
        )
        self.addStep(step)

    def update_config_file(self):
        step = MasterShellCommand(
            name='cp_config',
            command=[
                'bash',
                '-c',
                Interpolate(
                    'cp -v %s/benchmarker/configs/' % MASTERWORKDIR +
                    '*.conf ../configs/'
                )
            ]
        )
        self.addStep(step)


    def upload_benchmarker(self):
        self.addStep(FileDownload(Interpolate('%s/benchmarker.tar.gz' % MASTERWORKDIR), 'benchmarker.tar.gz', workdir='.'))

        self.addStep(ShellCommand(name='md5', command=['md5sum', 'benchmarker.tar.gz'], workdir='.'))
        self.addStep(ShellCommand(name='unpack_benchmarker', command=['tar', 'xf', 'benchmarker.tar.gz'], workdir='.'))
        self.addStep(ShellCommand(name='debug2', command=['ls', '-lha', 'benchmarker'], workdir='.'))
        self.addStep(MasterShellCommand(name="cleanup", command=['rm', '-rf', Interpolate(MASTERWORKDIR)]))

    def upload_credentials(self):
        self.addStep(FileDownload('benchmarkerCredentials', 'benchmarkerCredentials', workdir='benchmarker'))

    def clone_benchmarker(self):
        step = git.Git(
            repourl='https://github.com/xamarin/benchmarker/',
            workdir='benchmarker',
            branch='master',
            mode='incremental',
            # shallow=True,
            codebase='benchmarker',
            haltOnFailure=True
        )
        self.addStep(step)

    def clone_mono(self, guard):
        if guard is None:
            guard = lambda _: True

        step = git.Git(
            repourl='https://github.com/mono/mono/',
            workdir='mono',
            branch='master',
            mode='incremental',
            # shallow=True,
            codebase='mono',
            doStepIf=guard,
            haltOnFailure=True
        )
        self.addStep(step)

    def build_mono(self):
        self.addStep(
            ShellCommand(
                name='autogen.sh',
                command=['./autogen.sh'],
                workdir='mono'
            )
        )
        self.addStep(
            ShellCommand(
                name='configure',
                command=['bash', '-c', './configure --prefix=$PWD/build'],
                workdir='mono'
            )
        )
        self.addStep(ShellCommand(name='ccache_stats', command=['ccache', '-s']))
        self.addStep(
            ShellCommand(
                name='make',
                command=['make', '-j4', 'V=1'],
                workdir='mono'
            )
        )
        self.addStep(
            ShellCommand(
                name='make_install',
                command=['make', 'install'],
                workdir='mono'
            )
        )
        self.addStep(ShellCommand(name='ccache_stats', command=['ccache', '-s']))

    def maybe_create_runsetid(self, install_root):

        def _guard_runsetid_gen(step):
            if step.build.getProperties().has_key(PROPERTYNAME_RUNSETID):
                return step.build.getProperties()[PROPERTYNAME_RUNSETID] == ""
            return True

        def _guard_pullrequest_only(step):
            return step.build.getProperties().has_key(PROPERTYNAME_JENKINSGITHUBPULLREQUEST)

        self.clone_mono(guard=lambda s: _guard_runsetid_gen(s) and _guard_pullrequest_only(s))
        parsers = {
            PROPERTYNAME_RUNSETID: re.compile(r'"runSetId"\s*:\s*"(?P<' + PROPERTYNAME_RUNSETID + r'>\w+)"'),
            PROPERTYNAME_PULLREQUESTID: re.compile(r'"pullRequestId"\s*:\s*"(?P<' + PROPERTYNAME_PULLREQUESTID + r'>\w+)"')
        }
        self.addStep(
            CreateRunSetIdStep(
                install_root=install_root,
                name='create_RunSetId',
                parse_rules=parsers,
                workdir='benchmarker',
                doStepIf=_guard_runsetid_gen,
                haltOnFailure=True
            )
        )

    def report_github(self, secret_github_token):
        def _guard_pullrequest_only(step):
            return step.build.getProperties().has_key(PROPERTYNAME_JENKINSGITHUBPULLREQUEST)
        self.addStep(GithubWritePullrequestComment(name='report_github', githubuser='mono', githubrepo='mono', githubtoken=secret_github_token, doStepIf=_guard_pullrequest_only))

    def print_runsetid(self):
        self.addStep(
            ShellCommand(
                name='print_RunSetId',
                command=['echo', Interpolate("%(prop:" + PROPERTYNAME_RUNSETID + ")s")]
            )
        )

    def wipe(self):
        self.addStep(
            ShellCommand(
                name="wipe",
                description="wipe build dir",
                command=['bash', '-c', 'sudo /bin/rm -rf build'],
                workdir='.',
                alwaysRun=True
            )
        )


def disable_intel_turbo_steps():
    steps = []
    steps.append(
        ShellCommand(
            name="disableintelturbo",
            command=['bash', '-c', '(echo 0 | sudo /usr/bin/tee /sys/devices/system/cpu/cpufreq/boost) || (echo "only supported on Intel CPUs" && exit 1)'],
            haltOnFailure=True
        )
    )

    class AlwaysSuccessShellCommand(ShellCommand):
        def __init__(self, *args, **kwargs):
            ShellCommand.__init__(self, *args, **kwargs)

        def finished(self, _):
            ShellCommand.finished(self, SUCCESS)

    # cf. http://pm-blog.yarda.eu/2011/10/deeper-c-states-and-increased-latency.html
    # by keeping the file descriptor alive, we make sure that this setting is used.
    # after closing the file descriptor, the old setting will be restored by the
    # kernel module.
    steps.append(FileDownload('forcec0state.sh', 'forcec0state.sh'))

    # `setsid' is used in to escape the process group, otherwise it will be
    # killed by the timeout logic of AlwaysSuccessShellCommand. since the
    # parent process gets killed by it, we always force it to be
    # successful. (I wish there would be a nicer way to do it).
    steps.append(AlwaysSuccessShellCommand(
        name="forceC0state",
        command=['sudo', '-b', '/bin/bash', '-c', 'setsid bash -x ./forcec0state.sh'],
        haltOnFailure=False,
        flunkOnFailure=False,
        timeout=5
    ))

    return steps

def reset_intel_turbo_steps():
    steps = []
    steps.append(
        ShellCommand(
            name="enableturbo",
            command=['bash', '-c', '(echo 1 | sudo /usr/bin/tee /sys/devices/system/cpu/cpufreq/boost) || (echo "only supported on Intel CPUs" && exit 1)'],
            haltOnFailure=True,
            alwaysRun=True
        )
    )
    steps.append(
        ShellCommand(
            name="releaseNoTurboFP",
            command=['bash', '-c', 'sudo /bin/kill `sudo /usr/bin/lsof -t /dev/cpu_dma_latency`'],
            haltOnFailure=True,
            alwaysRun=True
        )
    )
    return steps


def gen_guard_benchmark_run(benchmark):
    def _benchmark_retry(benchmark, step):
        if not step.build.getProperties().has_key(PROPERTYNAME_SKIP_BENCHS):
            return True
        executed_benchmarks = step.build.getProperties().getProperty(PROPERTYNAME_SKIP_BENCHS)
        if executed_benchmarks is None:
            return True
        assert isinstance(executed_benchmarks, list), "it is: " + str(executed_benchmarks)
        return benchmark not in executed_benchmarks

    def _benchmark_filter(benchmark, step):
        if not step.build.getProperties().has_key(PROPERTYNAME_FILTER_BENCHS):
            return True
        filter_benchmarks = step.build.getProperties().getProperty(PROPERTYNAME_FILTER_BENCHS)
        if filter_benchmarks is None or len(filter_benchmarks) == 0:
            return True
        return benchmark in filter_benchmarks.split(',')

    # scopes in python are stupid.
    return lambda s: _benchmark_retry(benchmark, s) and _benchmark_filter(benchmark, s)


def benchmark_step(benchmark_name, commit_renderer, compare_args, root_renderer, attach_files=None):
    steps = []
    cmd = ['mono',
           'tools/compare.exe',
           '--benchmarks', benchmark_name,
           '--log-url', Interpolate(BUILDBOT_URL + '/builders/%(prop:buildername)s/builds/%(prop:buildnumber)s'),
           '--root', root_renderer(),
           '--commit', commit_renderer(),
           '--run-set-id', Interpolate('%(prop:' + PROPERTYNAME_RUNSETID + ')s'),
           '--config-file', Interpolate('configs/%(prop:config_name)s.conf')
          ]
    steps.append(
        ShellCommand(
            name=benchmark_name,
            description="benchmark " + benchmark_name,
            command=cmd + compare_args,
            timeout=45*60,
            doStepIf=gen_guard_benchmark_run(benchmark_name),
            logfiles=attach_files,
            workdir='benchmarker'
        )
    )
    return steps


from buildbot.interfaces import IRenderable
from zope.interface import implements

class DetermineMonoRevision(object):
    implements(IRenderable)
    #pylint: disable=C0103,R0201
    def getRenderingFor(self, props):
        if props.hasProperty('got_revision'):
            return props['got_revision']['mono']
        return "failed revision lookup"
    #pylint: enable=C0103,R0201

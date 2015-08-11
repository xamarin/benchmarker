from buildbot import interfaces
from buildbot.process.factory import BuildFactory
from buildbot.process.properties import Interpolate
from buildbot.steps.shell import ShellCommand
from buildbot.steps.master import MasterShellCommand
from buildbot.steps.transfer import FileDownload
from buildbot.steps.source import git
from buildbot.status.builder import SUCCESS

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
            name="build benchmarker",
            command=[
                'bash', '-x', '-c',
                Interpolate(
                    'pwd && ' +
                    'mkdir -p %s && ' % MASTERWORKDIR +
                    'cd %s && ' % MASTERWORKDIR +
                    'git clone --depth 1 -b master https://github.com/xamarin/benchmarker && ' +
                    'cd benchmarker/tools && (nuget restore tools.sln || nuget restore tools.sln) ' + #nuget crashes sometimes :-(
                    '&& xbuild && ' +
                    'cd ../.. && tar cvfz benchmarker.tar.gz benchmarker/tools/{*.dll,*.exe} && (md5 benchmarker.tar.gz || md5sum benchmarker.tar.gz)'
                )
                ]
        )
        self.addStep(step)

    def export_benchmark_list(self):
        step = MasterShellCommand(
            name="list benchmarks",
            command=[
                'bash', '-c', Interpolate(
                    'mono %s/benchmarker/tools/compare.exe --list-benchmarks | ' % MASTERWORKDIR +
                    'tee benchmark-%(prop:buildername)s.list')
            ]
        )
        self.addStep(step)

    def update_config_file(self):
        step = MasterShellCommand(
            name='cp config',
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
        self.addStep(ShellCommand(name='unpack benchmarker', command=['tar', 'xf', 'benchmarker.tar.gz'], workdir='.'))
        self.addStep(ShellCommand(name='debug2', command=['ls', '-lha', 'benchmarker'], workdir='.'))
        self.addStep(MasterShellCommand(name="cleanup", command=['rm', '-rf', Interpolate(MASTERWORKDIR)]))
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

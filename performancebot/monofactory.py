from buildbot import interfaces
from buildbot.process.factory import BuildFactory
from buildbot.process.properties import Interpolate
from buildbot.steps.shell import ShellCommand
from buildbot.steps.master import MasterShellCommand
from buildbot.steps.transfer import FileDownload
from buildbot.steps.source import git

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
        self.addStep(ExpandingStep(closure))

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

    def upload_benchmarker(self):
        self.addStep(FileDownload(Interpolate('%s/benchmarker.tar.gz' % MASTERWORKDIR), 'benchmarker.tar.gz', workdir='.'))

        self.addStep(ShellCommand(name='md5', command=['md5sum', 'benchmarker.tar.gz'], workdir='.'))
        self.addStep(ShellCommand(name='unpack benchmarker', command=['tar', 'xf', 'benchmarker.tar.gz'], workdir='.'))
        self.addStep(ShellCommand(name='debug2', command=['ls', '-lha', 'benchmarker'], workdir='.'))
        self.addStep(MasterShellCommand(name="cleanup", command=['rm', '-rf', Interpolate(MASTERWORKDIR)]))
        self.addStep(FileDownload('parse.pw', 'parse.pw', workdir='benchmarker'))

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


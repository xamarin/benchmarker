from buildbot.process.factory import BuildFactory
from buildbot.process.properties import Interpolate
from buildbot.steps.shell import ShellCommand
from buildbot.steps.master import MasterShellCommand
from buildbot.steps.transfer import FileDownload
from buildbot.steps.source import git

class DebianMonoBuildFactory(BuildFactory):
    def __init__(self, *args, **kwargs):
        BuildFactory.__init__(self, *args, **kwargs)

    def masterWorkDir(self):
        return 'tmp/%(prop:buildername)s/%(prop:buildnumber)s'

    def cloneAndBuildBenchmarkerOnMaster(self):
        s = MasterShellCommand(command = [
            'bash', '-x', '-c',
            Interpolate('pwd && ' +
            'mkdir -p %s && ' % self.masterWorkDir() +
            'cd %s && ' % self.masterWorkDir() +
            'git clone --depth 1 -b master https://github.com/xamarin/benchmarker && ' +
            'cd benchmarker/tools && (nuget restore tools.sln || nuget restore tools.sln) ' + #nuget crashes sometimes :-(
            '&& xbuild && ' +
            'cd ../.. && tar cvfz benchmarker.tar.gz benchmarker/tools/{*.dll,*.exe} && (md5 benchmarker.tar.gz || md5sum benchmarker.tar.gz)')
        ])
        self.addStep(s)

    def uploadBenchmarker(self):
        self.addStep(FileDownload(Interpolate('%s/benchmarker.tar.gz' % self.masterWorkDir()), 'benchmarker.tar.gz', workdir = '.'))

        self.addStep(ShellCommand(name = 'md5', command = ['md5sum', 'benchmarker.tar.gz'], workdir = '.'))
        self.addStep(ShellCommand(name = 'unpack benchmarker', command = ['tar', 'xf', 'benchmarker.tar.gz'], workdir = '.'))
        self.addStep(ShellCommand(name = 'debug2', command = ['ls', '-lha', 'benchmarker'], workdir = '.'))
        self.addStep(MasterShellCommand(name = "cleanup", command = ['rm', '-rf', Interpolate(self.masterWorkDir())]))

    def cloneBenchmarker(self):
        s = git.Git(
            repourl = 'https://github.com/xamarin/benchmarker/',
            workdir = 'benchmarker',
            branch = 'master',
            mode = 'incremental',
            # shallow = True,
            codebase = 'benchmarker',
            haltOnFailure = True
        )
        self.addStep(s)

    def wipe(self):
        self.addStep(
            ShellCommand(
                name = "wipe",
                description = "wipe build dir",
                command = ['bash', '-c', 'sudo /bin/rm -rf build'],
                workdir = '.',
                alwaysRun = True
            )
        )


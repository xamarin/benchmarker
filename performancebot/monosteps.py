from buildbot.steps.master import MasterShellCommand
from buildbot.steps.shell import ShellCommand
from buildbot.process.buildstep import LoggingBuildStep
from buildbot.status.builder import SUCCESS

from twisted.python import log

from constants import PROPERTYNAME_JENKINSBUILDURL, PROPERTYNAME_MONOVERSION, PROPERTYNAME_JENKINSGITHUBPULLREQUEST, PROPERTYNAME_JENKINSGITCOMMIT, PROPERTYNAME_PULLREQUESTID, PROPERTYNAME_COMPARE_JSON, PROPERTYNAME_BINARY_PROTOCOL_FILES, PROPERTYNAME_RUNIDS, MONO_SGEN_GREP_BINPROT_FILENAME

import json
import requests
import os
import tempfile


class ParsingShellCommand(ShellCommand):
    def __init__(self, parse_rules=None, *args, **kwargs):
        self.parse_rules = parse_rules if parse_rules is not None else {}
        for prop_name, regex in self.parse_rules.items():
            assert '<' + prop_name + '>' in regex.pattern

        ShellCommand.__init__(self, flunkOnFailure=True, *args, **kwargs)

    def evaluateCommand(self, cmd):
        result = ShellCommand.evaluateCommand(self, cmd)
        self._parse_result(cmd, result)
        return result

    def _parse_result(self, cmd, _):
        for prop_name, regex in self.parse_rules.items():
            results = []
            for log_text in cmd.logs.values():
                for match in regex.finditer(log_text.getText()):
                    value = match.group(prop_name)
                    log.msg("found " + str(prop_name) + ": " + str(value))
                    results.append(value)
            existing_value = self.getProperty(prop_name)
            if existing_value is not None:
                new_value = "None"
                if len(results) >= 1:
                    new_value = str(results[0])
                log.msg("overriding " + str(prop_name) + ", old value is: " + str(existing_value) + ", new value: " + new_value)
                self.setProperty(prop_name, None, 'ParsingShellCommand')
            assert len(results) <= 1, 'more than one match for %s: %s' % (prop_name, str(results))
            if len(results) >= 1:
                log.msg("ParsingShellCommand: " + prop_name + " <= " + str(results[0]))
                self.setProperty(prop_name, results[0], 'ParsingShellCommand')


class PutPropertiesStep(LoggingBuildStep):
    def __init__(self, properties, *args, **kwargs):
        self.properties = properties
        LoggingBuildStep.__init__(self, name='putproperties', *args, **kwargs)

    def start(self):
        for prop_name, value in self.properties.items():
            self.setProperty(prop_name, value)
        self.finished(SUCCESS)


class CreateRunSetIdStep(ParsingShellCommand):
    def __init__(self, install_root, *args, **kwargs):
        self.install_root = install_root
        ParsingShellCommand.__init__(self, *args, **kwargs)

    def start(self):
        pullrequestid = self.getProperty(PROPERTYNAME_JENKINSGITHUBPULLREQUEST)
        build_url = self.getProperty(PROPERTYNAME_JENKINSBUILDURL)
        mono_version = self.getProperty(PROPERTYNAME_MONOVERSION)
        git_commit = self.getProperty(PROPERTYNAME_JENKINSGITCOMMIT)
        config_name = self.getProperty('config_name')
        benchmarker_commit = self.getProperty('got_revision').get('benchmarker')
        assert benchmarker_commit is not None
        cmd = ['mono', 'tools/compare.exe', '--create-run-set']
        if pullrequestid is not None:
            cmd.append('--pull-request-url')
            cmd.append('https://github.com/mono/mono/pull/%s' % str(pullrequestid))
            cmd.append('--mono-repository')
            cmd.append('../mono')
        if build_url is not None:
            cmd.append('--build-url')
            cmd.append(build_url)
        if git_commit is not None:
            cmd.append('--main-product')
            cmd.append('mono')
            cmd.append(git_commit)
        cmd.append('--config-file')
        cmd.append('configs/%s.conf' % (config_name))
        cmd.append('--root')
        cmd.append(self.install_root(mono_version))
        cmd.append('--secondary-product')
        cmd.append('benchmarker')
        cmd.append(benchmarker_commit)
        self.setCommand(cmd)
        ShellCommand.start(self)


class GrabBinaryLogFilesStep(ShellCommand):
    def __init__(self, *args, **kwargs):
        ShellCommand.__init__(self, *args, **kwargs)

    def start(self):
        cmd_touch = ['touch']
        match = self.getProperty(PROPERTYNAME_COMPARE_JSON, "")
        if match == "" or match is None:
            self.setCommand(['echo', 'nothing todo'])
        else:
            j = json.loads(match)

            run_ids = [i['id'] for i in j['runs']]
            self.setProperty(PROPERTYNAME_RUNIDS, run_ids)

            bin_files = [i['binaryProtocolFile'].encode('ascii', 'ignore') for i in j['runs']]
            for bin_file in bin_files:
                self.addLogFile(os.path.basename(bin_file), bin_file)
                cmd_touch.append(bin_file)

            self.setCommand(['bash', '-c', "sleep 1; " + " ".join(cmd_touch) + "; sleep 1"])

        self.setProperty(PROPERTYNAME_COMPARE_JSON, None)
        ShellCommand.start(self)
        self.setProperty(PROPERTYNAME_BINARY_PROTOCOL_FILES, self._step_status.getLogs())


class ProcessBinaryProtocolFiles(MasterShellCommand):
    def __init__(self, *args, **kwargs):
        MasterShellCommand.__init__(self, *args, **kwargs)

    def start(self):
        log_temp_paths = self.getProperty(PROPERTYNAME_BINARY_PROTOCOL_FILES, [])
        runids = self.getProperty(PROPERTYNAME_RUNIDS, [])

        self.setProperty(PROPERTYNAME_BINARY_PROTOCOL_FILES, None)
        self.setProperty(PROPERTYNAME_RUNIDS, None)

        logs_full_path = []
        # don't use direct file handle, but let buildbot unchunk it for us.
        for log_file in log_temp_paths:
            if 'binprot' not in log_file.getFilename():
                continue
            with tempfile.NamedTemporaryFile('wb', delete=False) as f:
                f.write(log_file.getText())
                logs_full_path.append(f.name)

        logs_full_path.sort()

        if runids is None or runids == []:
            self.command = ['echo', 'nothing todo']
        else:
            masterworkdir = 'tmp/' + str(self.getProperty('buildername')) + '/' + str(self.getProperty('buildnumber'))
            compare_cmd = lambda logfile, runid: 'mono ' + masterworkdir + '/benchmarker/tools/compare.exe --upload-pause-times ' + logfile + ' --sgen-grep-binprot ' + MONO_SGEN_GREP_BINPROT_FILENAME + ' --run-id ' + str(runid) + ' || failed=1; rm -rf ' + logfile + '; '
            self.command = ['timeout', '--signal=15', '360', 'bash', '-x', '-c', 'failed=0; ' + "".join([compare_cmd(log_full_path, runid) for (log_full_path, runid) in zip(logs_full_path, runids)]) + ' if [ "$failed" == "1" ]; then exit 1; fi ']
        MasterShellCommand.start(self)


class GithubPostPRStatus(LoggingBuildStep):
    def __init__(self, githubuser, githubrepo, githubtoken, state, *args, **kwargs):
        self.githubuser = githubuser
        self.githubrepo = githubrepo
        self.githubtoken = githubtoken
        assert state in ["pending", "success", "error", "failure"]
        self.state = state
        LoggingBuildStep.__init__(self, *args, **kwargs)

    def start(self):
        parse_pullrequest_id = self.getProperty(PROPERTYNAME_PULLREQUESTID)
        config_name = self.getProperty('config_name')
        short_config_name = ''.join(map(lambda x: x[0], config_name.split('-')))
        platform = self.getProperty('platform')
        buildnumber = self.getProperty('buildnumber')
        pullrequest_commit_id = self.getProperty(PROPERTYNAME_JENKINSGITCOMMIT)

        headers = {}
        headers['content-type'] = 'application/json'
        headers['Authorization'] = 'token ' + self.githubtoken
        data = {}
        data['state'] = self.state
        data['description'] = self.state
        data['context'] = 'perf/%s_%s/%s' % (platform, short_config_name, str(buildnumber))
        data['target_url'] = 'http://xamarin.github.io/benchmarker/front-end/pullrequest.html#id=%s' % str(parse_pullrequest_id)

        r = requests.post(
            'https://api.github.com/repos/%s/%s/statuses/%s' % (self.githubuser, self.githubrepo, str(pullrequest_commit_id)),
            headers=headers,
            data=json.dumps(data)
        )
        log.msg("GithubPostPRStatus: status_code(" + str(r.status_code) + "), text: \"" + str(r.text) + "\"")
        self.finished(SUCCESS)


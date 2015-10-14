from buildbot.steps.shell import ShellCommand
from buildbot.process.buildstep import LoggingBuildStep
from buildbot.status.builder import SUCCESS

from twisted.python import log

from constants import PROPERTYNAME_JENKINSBUILDURL, PROPERTYNAME_MONOVERSION, PROPERTYNAME_JENKINSGITHUBPULLREQUEST, PROPERTYNAME_JENKINSGITCOMMIT, BUILDBOT_URL, PROPERTYNAME_PULLREQUESTID

import json
import requests


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
                log.msg("overriding " + str(prop_name) + ", old value is: " + str(existing_value) + ", new value: " + str(results[0]))
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
        self.setCommand(cmd)
        ShellCommand.start(self)

class GithubWritePullrequestComment(LoggingBuildStep):
    def __init__(self, githubuser, githubrepo, githubtoken, *args, **kwargs):
        self.githubuser = githubuser
        self.githubrepo = githubrepo
        self.githubtoken = githubtoken
        LoggingBuildStep.__init__(self, *args, **kwargs)

    def start(self):
        parse_pullrequest_id = self.getProperty(PROPERTYNAME_PULLREQUESTID)
        buildername = self.getProperty('buildername')
        buildnumber = self.getProperty('buildnumber')
        pullrequest_id = self.getProperty(PROPERTYNAME_JENKINSGITHUBPULLREQUEST)

        payload = [
            '`<botmode>`',
            'Benchmark results: http://xamarin.github.io/benchmarker/front-end/pullrequest.html#id=%s' % str(parse_pullrequest_id),
            'buildbot logs: %s/builders/%s/builds/%s' % (BUILDBOT_URL, buildername, str(buildnumber)),
            '`</botmode>`'
        ]
        requests.post(
            'https://api.github.com/repos/%s/%s/issues/%s/comments' % (self.githubuser, self.githubrepo, str(pullrequest_id)),
            headers={'content-type': 'application/json', 'Authorization': 'token ' + self.githubtoken},
            data=json.dumps({'body': '\n'.join(payload)})
        )
        self.finished(SUCCESS)

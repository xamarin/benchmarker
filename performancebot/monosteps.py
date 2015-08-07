from buildbot.steps.shell import ShellCommand
from buildbot.process.buildstep import LoggingBuildStep
from buildbot.status.builder import SUCCESS

from twisted.python import log

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
            assert len(results) == 1, 'more than one match: ' + str(results)
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


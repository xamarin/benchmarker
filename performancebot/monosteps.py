from buildbot.steps.shell import ShellCommand
from buildbot.process.buildstep import LoggingBuildStep
from buildbot.status.builder import SUCCESS

from twisted.python import log

class ParsingShellCommand(ShellCommand):
    def __init__(self, parseRules={}, maxTime = 3 * 3600, *args, **kwargs):
        self.parseRules = parseRules
        ShellCommand.__init__(self, flunkOnFailure = True, maxTime = maxTime, *args, **kwargs)

    def evaluateCommand(self, cmd):
        result = ShellCommand.evaluateCommand(self, cmd)
        self._parseResult(cmd, result)
        return result

    def _parseResult(self, cmd, result):
        for propertyName, regex in self.parseRules.items():
            results = []
            for logText in cmd.logs.values():
                for match in regex.finditer(logText.getText()):
                    results.append(match.group(0))
            existingValue = self.getProperty(propertyName)
            assert existingValue is None, 'property has already value: ' + str(existingValue) + ', trying to replace it with: ' + str(results)
            assert len(results) == 1, 'more than one match: ' + str(results)
            log.msg("ParsingShellCommand: " + propertyName + " <= " + str(results[0]))
            self.setProperty(propertyName, results[0], 'ParsingShellCommand')


class PutPropertiesStep(LoggingBuildStep):
    def __init__(self, properties, *args, **kwargs):
        self.properties = properties
        LoggingBuildStep.__init__(self, name = 'putproperties', *args, **kwargs)

    def start(self):
        for k, v in self.properties.items():
            self.setProperty(k, v)
        self.finished(SUCCESS)


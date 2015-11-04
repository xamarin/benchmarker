# define __repr__ for LogFile class
from buildbot.status.logfile import LogFile

def logfile_to_string(self):
    return "<logfile=" + self.getFilename() + ">"


# special handle LogFile in json encoder
import json

def json_encoder_default(self, o):
    if isinstance(o, LogFile):
        return str(o)
    else:
        raise TypeError(repr(o) + " is not JSON serializable")


# entry point
def apply_all_monkeypatches():
    LogFile.__repr__ = logfile_to_string
    json.JSONEncoder.default = json_encoder_default

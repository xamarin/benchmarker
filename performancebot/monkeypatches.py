###########################################################
# define __repr__ for LogFile class
from buildbot.status.logfile import LogFile

def logfile_to_string(self):
    return "<logfile=" + self.getFilename() + ">"

###########################################################
# special handle LogFile in json encoder
import json

def json_encoder_default(_, o):
    if isinstance(o, LogFile):
        return str(o)
    else:
        raise TypeError(repr(o) + " is not JSON serializable")

###########################################################
# longer expiration
from buildbot.status.web.session import Session
from datetime import datetime, timedelta

def renew_longer(self):
    # expires in 60 days
    self.expiration = datetime.now() + timedelta(60)
    return self.expiration

###########################################################
# entry point
def apply_all_monkeypatches():
    LogFile.__repr__ = logfile_to_string
    json.JSONEncoder.default = json_encoder_default
    Session.renew = renew_longer

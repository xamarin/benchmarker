from buildbot.status.logfile import LogFile

def logfile_to_string(self):
    return "<logfile=" + self.getFilename() + ">"




def apply_all_monkeypatches():
    LogFile.__repr__ = logfile_to_string

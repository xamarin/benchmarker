import json
import sys
import os.path

from subprocess import call


CACHE_FILE = "benchmarkerCredentials"

def get_slaves():
    return _cache_content()[_get_accredit_key(get_slaves)].items()
get_slaves.accredit_key = 'pbotSlaves'


def get_slack_api_key():
    return _cache_content()[_get_accredit_key(get_slack_api_key)]['url']
get_slack_api_key.accredit_key = 'pbotSlackAPI'


def get_google_api_key_clientid():
    return _cache_content()[_get_accredit_key(get_google_api_key_clientid)]['clientid']
get_google_api_key_clientid.accredit_key = 'pbotGoogleOAuth'


def get_google_api_key_clientsecret():
    return _cache_content()[_get_accredit_key(get_google_api_key_clientsecret)]['clientsecret']
get_google_api_key_clientsecret.accredit_key = 'pbotGoogleOAuth'


def get_pb_user():
    return _cache_content()[_get_accredit_key(get_pb_user)]['user']
get_pb_user.accredit_key = 'pbotBisector'


def get_pb_password():
    return _cache_content()[_get_accredit_key(get_pb_password)]['password']
get_pb_password.accredit_key = 'pbotBisector'


def get_github_write_pr_comments():
    return _cache_content()[_get_accredit_key(get_github_write_pr_comments)]['writePullRequestCommentsToken']
get_github_write_pr_comments.accredit_key = 'gitHub'


# dummy methods for credentials that are needed by the slaves (e.g. compare.exe or find-regression tool)
def slave_benchmarker():
    pass
slave_benchmarker.accredit_key = 'benchmarker'

def slave_postgresql():
    pass
slave_postgresql.accredit_key = 'benchmarkerPostgres'

def slave_github():
    pass
slave_github.accredit_key = 'gitHub'

def slave_slack():
    pass
slave_slack.accredit_key = 'regressionSlack'

def slave_xtcapikey():
    pass
slave_xtcapikey.accredit_key = 'xtcapikey'

def slave_httpAPITokens():
    pass
slave_httpAPITokens.accredit_key = 'httpAPITokens'




def _cache_content():
    def _decode_list(data):
        rvs = []
        for item in data:
            if isinstance(item, unicode):
                item = item.encode('utf-8')
            elif isinstance(item, list):
                item = _decode_list(item)
            elif isinstance(item, dict):
                item = _decode_dict(item)
            rvs.append(item)
        return rvs

    def _decode_dict(data):
        rvs = {}
        for key, value in data.iteritems():
            if isinstance(key, unicode):
                key = key.encode('utf-8')

            if isinstance(value, unicode):
                value = value.encode('utf-8')
            elif isinstance(value, list):
                value = _decode_list(value)
            elif isinstance(value, dict):
                value = _decode_dict(value)
            rvs[key] = value
        return rvs

    if not os.path.isfile(CACHE_FILE):
        return None
    with open(CACHE_FILE) as filedescriptor:
        return json.load(filedescriptor, object_hook=_decode_dict)


def _cache_has_entry(service_name):
    cache = _cache_content()
    if not cache:
        return False
    return _cache_content().has_key(service_name)


def _request_service_creds(accredit, service_name):
    call(['mono', accredit, service_name])

def _get_accredit_key(fun_ref):
    return getattr(fun_ref, 'accredit_key', None)


if __name__ == "__main__":
    if len(sys.argv) <= 1 or len(sys.argv) > 2 or not os.path.isfile(sys.argv[1]):
        print "usage: ./" + sys.argv[0] + " <path-to-Accreditize-binary>"
        sys.exit(1)

    for fun_name, fun_val in dict(sys.modules[__name__].__dict__).iteritems():
        if not callable(fun_val):
            continue
        accredit_service_name = _get_accredit_key(fun_val)
        if accredit_service_name is None:
            continue

        print "found method: " + fun_name
        if _cache_has_entry(accredit_service_name):
            print "\t> credentials up to date!"
        else:
            print "\t> requesting credentials for " + accredit_service_name
            _request_service_creds(sys.argv[1], accredit_service_name)
        print "calling " + fun_name + ":"
        print "\t" + str(fun_val())
        print ""

# Copyright (c) 2015, Patrick Uiterwijk <puiterwijk@redhat.com>
# Copyright (c) 2016, Bernhard Urban <bernhard.urban@xamarin.com>
# All rights reserved
#
# This file is part of Buildbot-GithubOAuth2.  Buildbot-GithubOAuth2 is free
# software: you can redistribute it and/or modify it under the terms of the GNU
# General Public License as published by the Free Software Foundation, version
# 2.
#
# This program is distributed in the hope that it will be useful, but WITHOUT
# ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
# FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more
# details.
#
# You should have received a copy of the GNU General Public License along with
# this program; if not, write to the Free Software Foundation, Inc., 51
# Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.

#coding style by upstream doesn't match with pylint

from buildbot.status.web.authz import Authz
from buildbot.status.web.session import SessionManager
from twisted.internet import defer
from twisted.web import resource
from twisted.web.util import redirectTo

from urllib import urlencode
from urlparse import parse_qs
import json
import requests

# from openid.consumer import consumer
# from openid.fetchers import setDefaultFetcher, Urllib2Fetcher
# from openid.extensions import sreg
# from openid_cla import cla
# from openid_teams import teams

COOKIE_KEY = "BuildBotGithubOAuth2Session"
AUTHURI = 'https://github.com/login/oauth/authorize'
TOKENURI = 'https://github.com/login/oauth/access_token'
RESOURCEENDPOINT = 'https://api.github.com'

class GithubOAuth2Handler(resource.Resource):
    def __init__(self, authz):
        self.authz = authz
        resource.Resource.__init__(self)

    def render_GET(self, request):
        error = request.args.get("error", [""])[0]
        code = request.args.get("code", [""])[0]
        if error:
            return str(error)
        elif not code:
            return redirectTo(self.authz.getLoginURL(), request)
        else:
            details = self.verifyCode(code)
            if 'xamarin' not in details['groups']:
                return "no xamarin employee, no power"
            cookie, s = self.authz.sessions.new(details['userName'], details)
            request.addCookie(COOKIE_KEY, cookie, expires=s.getExpiration(), path="/")
            request.received_cookies = {COOKIE_KEY: cookie}
            return redirectTo(self.authz.root_uri, request)

    def verifyCode(self, code):
        data = {'redirect_uri': self.authz.root_uri + '_github_oauth2_handler',
                'grant_type': 'authorization_code',
                'code': code,
                'client_id': self.authz.client_id,
                'client_secret': self.authz.client_secret
               }
        response = requests.post(TOKENURI, data=data, auth=None)
        if isinstance(response.content, basestring):
            try:
                content = json.loads(response.content)
            except ValueError:
                content = parse_qs(response.content)
                for key, value in content.items():
                    content[key] = value[0]
        else:
            assert False, "what happened?"
        session = createSessionFromToken(content)
        return getUserInfoFromOAuthClient(session)

def createSessionFromToken(token):
    s = requests.Session()
    s.params = {'access_token': token['access_token']}
    return s

def getUserInfo(session, path):
    ret = session.get(RESOURCEENDPOINT + path)
    return ret.json()

def getUserInfoFromOAuthClient(c):
    user = getUserInfo(c, '/user')
    orgs = getUserInfo(c, '/users/' + user['login'] + '/orgs')
    return dict(
        fullName=user['name'],
        userName=user['login'],
        email=user['email'],
        groups=[org['login'] for org in orgs]
    )


class GithubOAuth2AuthZ(object):
    """Decide who can do what."""
    def __init__(self, url, client_id, client_secret, root_uri, **kwargs):
        unknown = []
        self.permissions = {}
        for group in kwargs:
            self.permissions[group] = []
            for perm in kwargs[group]:
                if perm in Authz.knownActions:
                    self.permissions[group].append(perm)
                else:
                    unknown.append(perm)

        self.url = url
        self.client_id = client_id
        self.client_secret = client_secret
        self.root_uri = root_uri
        self.sessions = SessionManager()
        self.init_childs = False
        # This makes us get self.master as per baseweb.py:472
        self.auth = self
        # This makes the login form be a link
        self.useHttpHeader = True
        self.httpLoginUrl = '/_github_oauth2_handler'

        if unknown != []:
            raise ValueError('Unknown authorization action(s) ' +
                             ', '.join(unknown))

    def session(self, request):
        if COOKIE_KEY in request.received_cookies:
            cookie = request.received_cookies[COOKIE_KEY]
            return self.sessions.get(cookie)
        return None

    def authenticated(self, request):
        return self.session(request) is not None

    def getUserInfo(self, user):
        s = self.sessions.getUser(user)
        if s:
            return s.infos
        return None

    def getUsername(self, request):
        """Get the userid of the user"""
        s = self.session(request)
        if s:
            return s.user
        return '<unknown>'

    def getUsernameHTML(self, request):
        """Get the user formatted in html (with possible link to email)"""
        s = self.session(request)
        if s:
            return s.userInfosHTML()
        return "not authenticated?!"

    def getUsernameFull(self, request):
        """Get the full username as fullname <email>"""
        s = self.session(request)
        if s:
            return "%(fullName)s <%(email)s>" % (s.infos)
        else:
            return request.args.get("username", ["<unknown>"])[0]

    def create_childs(self, request):
        # We need to create the childs with this workaround
        #  because we won't get the site information prior
        #  to handling the very first request
        if not self.init_childs:
            self.init_childs = True
            request.site.resource.putChild('_github_oauth2_handler', GithubOAuth2Handler(self))

    def shouldAllowAction(self, action, request):
        self.create_childs(request)
        if action in self.permissions.get('all', []):
            return True

        s = self.getUsername(request)
        if s and not s == '<unknown>':
            if action in self.permissions.get('authenticated', []):
                return True
        return False

    def advertiseAction(self, action, request):
        """Should the web interface even show the form for ACTION?"""
        if action not in Authz.knownActions:
            raise KeyError("unknown action")
        return self.shouldAllowAction(action, request)

    def actionAllowed(self, action, request, *_):
        """Is this ACTION allowed, given this http REQUEST?"""
        if action not in Authz.knownActions:
            raise KeyError("unknown action")
        return defer.succeed(self.shouldAllowAction(action, request))

    def logout(self, request):
        if COOKIE_KEY in request.received_cookies:
            cookie = request.received_cookies[COOKIE_KEY]
            self.sessions.remove(cookie)

    def getLoginURL(self):
        oauth_params = {'redirect_uri': self.root_uri + '_github_oauth2_handler',
                        'client_id': self.client_id,
                        'response_type': 'code',
                        'access_type': 'offline'
                       }

        return "%s?%s" % (AUTHURI, urlencode(oauth_params))


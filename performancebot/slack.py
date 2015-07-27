import json
import requests

from buildbot.status.base import StatusReceiverMultiService
from buildbot.status.builder import Results, SUCCESS


class StatusPush(StatusReceiverMultiService):

    def __init__(self, url, username=None, channel=None, localhost_replace=False, **kwargs):
        StatusReceiverMultiService.__init__(self)
        self.url = url
        self.username = username
        self.channel = channel
        self.localhost_replace = localhost_replace

    def setServiceParent(self, parent):
        StatusReceiverMultiService.setServiceParent(self, parent)
        self.master_status = self.parent
        self.master_status.subscribe(self)
        self.master = self.master_status.master

    def disownServiceParent(self):
        self.master_status.unsubscribe(self)
        self.master_status = None
        for w in self.watched:
            w.unsubscribe(self)
        return StatusReceiverMultiService.disownServiceParent(self)

    def builderAdded(self, name, builder):
        return self  # subscribe to this builder

    def buildFinished(self, builderName, build, result):
        url = self.master_status.getURLForThing(build)
        if self.localhost_replace:
            url = url.replace("//localhost", "//%s" % self.localhost_replace)

        message = "%s - %s - <%s>" % \
            (builderName, Results[result].upper(), url)
        payload = {
            'text': message
        }
        if self.username:
            payload['username'] = self.username
        if self.channel:
            payload['channel'] = self.channel
        if result == SUCCESS:
            payload['icon_emoji'] = ':sunglasses:'
        else:
            payload['icon_emoji'] = ':skull:'

        requests.post(
            self.url,
            headers={
                'content-type': 'application/json'
            },
            data=json.dumps(payload)
        )

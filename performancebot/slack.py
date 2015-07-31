import json
import requests

from buildbot.status.base import StatusReceiverMultiService
from buildbot.status.builder import Results, SUCCESS, EXCEPTION, RETRY


class StatusPush(StatusReceiverMultiService):

    def __init__(self, url, username=None, channel=None, localhost_replace=False, *args, **kwargs):
        self.url = url
        self.username = username
        self.channel = channel
        self.localhost_replace = localhost_replace
        self.master_status = None
        StatusReceiverMultiService.__init__(self, *args, **kwargs)

    def setServiceParent(self, parent):
        StatusReceiverMultiService.setServiceParent(self, parent)
        self.master_status = self.parent
        self.master_status.subscribe(self)

    def disownServiceParent(self):
        self.master_status.unsubscribe(self)
        self.master_status = None
        # for w in self.watched:
        #     w.unsubscribe(self)
        return StatusReceiverMultiService.disownServiceParent(self)

    #pylint: disable=W0613
    def builderAdded(self, name, builder):
        return self  # subscribe to this builder
    #pylint: enable=W0613

    def buildFinished(self, builderName, build, result):
        #pylint: disable=E1101
        url = self.master_status.getURLForThing(build)
        #pylint: enable=E1101
        if self.localhost_replace:
            url = url.replace("http://localhost:8010", self.localhost_replace)

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
            payload['icon_emoji'] = ':doge:'
        elif result == EXCEPTION:
            payload['icon_emoji'] = ':collision:'
        elif result == RETRY:
            payload['icon_emoji'] = ':feet:'
        else:
            payload['icon_emoji'] = ':skull:'

        requests.post(
            self.url,
            headers={
                'content-type': 'application/json'
            },
            data=json.dumps(payload)
        )

"use strict";

/* global Parse */

// These two lines are required to initialize Express in Cloud Code.
var express = require('express');
var querystring = require('querystring');
var credentials = require ('cloud/credentials');
var app = express();

var githubRedirectEndpoint = 'https://github.com/login/oauth/authorize?';
var githubValidateEndpoint = 'https://github.com/login/oauth/access_token';
var githubUserOrgsEndpoint = 'https://api.github.com/user/orgs';

var Credentials = Parse.Object.extend ("Credentials");
var CredentialsRequest = Parse.Object.extend ("CredentialsRequest");
var CredentialsResponse = Parse.Object.extend ("CredentialsResponse");

var restrictedAcl = new Parse.ACL ();
restrictedAcl.setPublicReadAccess (false);
restrictedAcl.setPublicWriteAccess (false);

// Global app configuration section
app.set('views', 'cloud/views');  // Specify the folder to find templates
app.set('view engine', 'ejs');    // Set the template engine
app.use(express.bodyParser());    // Middleware for reading request body

/**
 * This function is called when GitHub redirects the user back after
 *   authorization.  It calls back to GitHub to validate and exchange the code
 *   for an access token.
 */
var getGitHubAccessToken = function (code) {
    var body = querystring.stringify({
        'client_id': credentials.githubClientId,
        'client_secret': credentials.githubClientSecret,
        'code': code
    });
    return Parse.Cloud.httpRequest({
        method: 'POST',
        url: githubValidateEndpoint,
        headers: {
            'Accept': 'application/json',
            'User-Agent': 'Parse.com Cloud Code'
        },
        body: body
    });
};

var getGitHubEndpoint = function (endpoint, accessToken) {
    return Parse.Cloud.httpRequest({
        method: 'GET',
        url: endpoint,
        params: { 'access_token': accessToken },
        headers: {
            'User-Agent': 'Parse.com Cloud Code'
        }
    });
};

var destroyRequestAndResponse = function (credentialsRequest, credentialsResponse) {
    var promises = [];

    if (credentialsRequest !== undefined)
        promises.push (credentialsRequest.destroy ());

    if (credentialsResponse !== undefined)
        promises.push (credentialsResponse.destroy ());

    return Parse.Promise.when (promises);
};

var requestCredentialsHandler = function (data, res) {
    if (!(data && data.service && data.key && data.secret)) {
        res.send (400, "Error: Missing service, key, or secret.");
        return;
    }

    var credentialsObject;

    var credentialsQuery = new Parse.Query (Credentials);
    credentialsQuery.equalTo ('service', data.service);

    credentialsQuery.find ({ useMasterKey: true }).then (function (results) {
        if (results.length === 0)
            return Parse.Promise.error ('Service does not exist');
        credentialsObject = results [0];

        // Ensure there's no entry with that key already.
        var requestQuery = new Parse.Query (CredentialsRequest);
        requestQuery.equalTo ('key', data.key);
        return requestQuery.find ({ useMasterKey: true });
    }).then (function (results) {
        if (results.length > 0)
            return Parse.Promise.error ('Key already exists');

        var credentialsRequest = new CredentialsRequest ();
        credentialsRequest.setACL (restrictedAcl);
        credentialsRequest.set ('credentials', credentialsObject);
        credentialsRequest.set ('key', data.key);
        credentialsRequest.set ('secret', data.secret);
        return credentialsRequest.save (null, { useMasterKey: true });
    }).then (function (obj) {
        res.type ('text/plain');
        res.send (githubRedirectEndpoint + querystring.stringify ({
            'client_id': credentials.githubClientId,
            'state': obj.id,
            'scope': 'read:org'
        }));
    }, function (error) {
        // FIXME: More appropriate error codes
        res.send (400, "Error: " + JSON.stringify (error));
    });
};

app.post ('/requestCredentials', function (req, res) {
    return requestCredentialsHandler (req.body, res);
});

app.get ('/oauthCallback', function (req, res) {
    var data = req.query;

    if (!(data && data.code && data.state)) {
        res.send (400, "Error: Missing code or state.");
        return;
    }

    var query = new Parse.Query (CredentialsRequest);
    Parse.Cloud.useMasterKey();
    Parse.Promise.as ().then (function () {
        return query.get (data.state);
    }).then (function (credentialsRequest) {
        // FIXME: Make this nicer
        res.render ('oauthConfirm', {
            key: credentialsRequest.get ('key'),
            state: data.state,
            code: data.code
        });
    }, function (error) {
        // FIXME: More appropriate error codes
        res.send (400, "Error: " + JSON.stringify (error));
    });
});

app.post ('/oauthFollowup', function (req, res) {
    var data = req.body;
    var token;
    var credentialsRequest;

    if (!(data && data.code && data.state)) {
        res.send (400, "Error: Missing code or state.");
        return;
    }

    var query = new Parse.Query (CredentialsRequest);
    Parse.Cloud.useMasterKey();
    Parse.Promise.as ().then (function () {
        return query.get (data.state);
    }).then (function (obj) {
        credentialsRequest = obj;
        return getGitHubAccessToken (data.code);
    }).then (function (access) {
        var githubData = access.data;
        if (githubData && githubData.access_token && githubData.token_type) {
            token = githubData.access_token;
            return getGitHubEndpoint (githubUserOrgsEndpoint, token);
        } else {
            return Parse.Promise.error ("Invalid access request.");
        }
    }).then (function (organizationsResponse) {
        var organizationsData = organizationsResponse.data;
        var found = false;
        for (var i = 0; i < organizationsData.length; ++i) {
            if (organizationsData [i].login === "xamarin") {
                found = true;
                break;
            }
        }
        if (!found) {
            return Parse.Promise.error ("Not a member of Xamarin.");
        }

        var credentialsResponse = new CredentialsResponse ();
        credentialsResponse.set ('key', credentialsRequest.get ('key'));
        credentialsResponse.set ('success', true);
        return credentialsResponse.save (null, { useMasterKey: true });
    }).then (function (obj) {
        // FIXME: Make a nice page
        res.render ('hello', { message: "id: " + obj.id });
    }, function (error) {
        // FIXME: More appropriate error codes
        res.send (400, "Error: " + JSON.stringify (error));
    });
});

app.post ('/getCredentials', function (req, res) {
    var data = req.body;
    var credentialsRequest;
    var credentialsResponse;
    var responseString;
    var responseContentType;

    var requestQuery = new Parse.Query (CredentialsRequest);
    var responseQuery = new Parse.Query (CredentialsResponse);
    Parse.Cloud.useMasterKey ();

    if (!(data && data.key && data.secret)) {
        res.send (400, "Error: Missing key or secret.");
        return;
    }

    requestQuery.equalTo ('key', data.key).include ('credentials');
    responseQuery.equalTo ('key', data.key);

    Parse.Promise.as ().then (function () {
        return requestQuery.first ();
    }).then (function (obj) {
        if (!obj)
            return Parse.Promise.error ('Request not found.');
        credentialsRequest = obj;
        return responseQuery.first ();
    }).then (function (obj) {
        if (!obj)
            return Parse.Promise.error ('Response not found.');
        credentialsResponse = obj;
        if (credentialsRequest.get ('secret') !== data.secret)
            return Parse.Promise.error ('Secret does not match.');
        if (!credentialsResponse.get ('success'))
            return Parse.Promise.error ('Request not successful.');

        var credentialsObject = credentialsRequest.get ('credentials');
        responseString = credentialsObject.get ('responseString');
        responseContentType = credentialsObject.get ('responseContentType');

        return destroyRequestAndResponse (credentialsRequest, credentialsResponse);
    }).then (function () {
        res.type (responseContentType);
        res.send (responseString);
    }, function (error) {
        // FIXME: More appropriate error codes
        res.send (400, "Error: " + JSON.stringify (error));

        return destroyRequestAndResponse (credentialsRequest, credentialsResponse);
    });
});

var cleanupCredentialsJob = function (params, status) {
    var now = new Date ();
    var before = new Date (now.getTime () - 5 * 60 * 1000);
    var query;
    if (params.table === 'request')
        query = new Parse.Query (CredentialsRequest);
    else
        query = new Parse.Query (CredentialsResponse);
    query.lessThan ("createdAt", before);

    query.find ({ useMasterKey: true }).then (function (results) {
        if (results.length > 0)
            return Parse.Object.destroyAll (results, { useMasterKey: true });
        return Parse.Promise.as ();
    }).then (function () {
        status.success ("Credentials cleaned up!");
    }, function (error) {
        status.error ("Error cleaning up credentials: " + JSON.stringify (error));
    });
};

Parse.Cloud.job ("cleanupCredentialRequests", function (request, status) {
    return cleanupCredentialsJob ({ table: 'request' }, status);
});

Parse.Cloud.job ("cleanupCredentialResponses", function (request, status) {
    return cleanupCredentialsJob ({ table: 'response' }, status);
});

// Attach the Express app to Cloud Code.
app.listen();

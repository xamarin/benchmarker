
// These two lines are required to initialize Express in Cloud Code.
var  express = require('express');
var querystring = require('querystring');
app = express();

var githubRedirectEndpoint = 'https://github.com/login/oauth/authorize?';
var githubValidateEndpoint = 'https://github.com/login/oauth/access_token';
var githubUserEndpoint = 'https://api.github.com/user';
var githubUserOrgsEndpoint = 'https://api.github.com/user/orgs';

var githubClientId = '6f1489f7dfb98d5ae423';
var githubClientSecret = 'bd100b747f6bfe0ca2005603ba3d189032d798d8';

var CredentialsRequest = Parse.Object.extend ("CredentialsRequest");
var CredentialsResponse = Parse.Object.extend ("CredentialsResponse");

var restrictedAcl = new Parse.ACL ();
restrictedAcl.setPublicReadAccess (false);
restrictedAcl.setPublicWriteAccess (false);

// Global app configuration section
app.set('views', 'cloud/views');  // Specify the folder to find templates
app.set('view engine', 'ejs');    // Set the template engine
app.use(express.bodyParser());    // Middleware for reading request body

// This is an example of hooking up a request handler with a specific request
// path and HTTP verb using the Express routing API.
app.get('/hello', function(req, res) {
  res.render('hello', { message: 'Congrats, you just set up your app!' });
});

app.get ('/requestCredentials', function (req, res) {
    var data = req.query;

    if (!(data && data.service && data.key && data.secret)) {
        res.render ('hello', { message: 'Must have service, key, and secret.' });
        return;
    }

    if (data.service !== 'benchmarker') {
        res.render ('hello', { message: 'Unknown service.' });
        return;
    }

    // FIXME: Ensure there's no entry with that key already.

    var credentialsRequest = new CredentialsRequest ();
    credentialsRequest.setACL (restrictedAcl);
    credentialsRequest.set ('service', data.service);
    credentialsRequest.set ('key', data.key);
    credentialsRequest.set ('secret', data.secret);

    credentialsRequest.save (null, { useMasterKey: true }).then (function (obj) {
        res.type ('text/plain');
        res.send (githubRedirectEndpoint + querystring.stringify ({
            client_id: githubClientId,
            state: obj.id,
            scope: 'read:org'
        }));
    }, function (error) {
        res.render ('hello', { message: "Error: " + JSON.stringify (error) });
    });
});

app.get ('/oauthCallback', function (req, res) {
    var data = req.query;

    if (!(data && data.code && data.state)) {
        res.render ('hello', { message: 'Something went wrong.'});
        return;
    }

    var query = new Parse.Query (CredentialsRequest);
    Parse.Cloud.useMasterKey();
    Parse.Promise.as ().then (function () {
        return query.get (data.state);
    }).then (function (credentialsRequest) {
        res.render ('oauthConfirm', {
            key: credentialsRequest.get ('key'),
            state: data.state,
            code: data.code
        });
    }, function (error) {
        res.render ('hello', { message: "Error: " + JSON.stringify (error) });
    });
});

app.post ('/oauthFollowup', function (req, res) {
    var data = req.body;
    var token;
    var userData;
    var credentialsRequest;

    if (!(data && data.code && data.state)) {
        res.render ('hello', { message: 'Something went wrong: ' + JSON.stringify (data) });
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
        res.render ('hello', { message: "id: " + obj.id });
    }, function (error) {
        res.render ('hello', { message: "Error: " + JSON.stringify (error) });
    });
});

app.get ('/getCredentials', function (req, res) {
    var data = req.query;
    var credentialsRequest;
    var credentialsResponse;
    var credentials;

    var requestQuery = new Parse.Query (CredentialsRequest);
    var responseQuery = new Parse.Query (CredentialsResponse);
    Parse.Cloud.useMasterKey ();

    if (!(data && data.key && data.secret)) {
        res.render ('hello', { message: 'Must have key and secret.' });
        return;
    }

    requestQuery.equalTo ('key', data.key);
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

        credentials = 'Supersecret credentials';

        return destroyRequestAndResponse (credentialsRequest, credentialsResponse);
    }).then (function () {
        res.type ('text/plain');
        res.send (credentials);
    }, function (error) {
        res.render ('hello', { message: "Error: " + JSON.stringify (error) });

        return destroyRequestAndResponse (credentialsRequest, credentialsResponse);
    });
});

/**
 * This function is called when GitHub redirects the user back after
 *   authorization.  It calls back to GitHub to validate and exchange the code
 *   for an access token.
 */
var getGitHubAccessToken = function(code) {
  var body = querystring.stringify({
    client_id: githubClientId,
    client_secret: githubClientSecret,
    code: code
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
}

var getGitHubEndpoint = function (endpoint, accessToken) {
    return Parse.Cloud.httpRequest({
      method: 'GET',
      url: endpoint,
      params: { access_token: accessToken },
      headers: {
        'User-Agent': 'Parse.com Cloud Code'
      }
    });
}

var getGitHubUserDetails = function(accessToken) {
    return getGitHubEndpoint (githubUserEndpoint, accessToken);
}

var destroyRequestAndResponse = function (credentialsRequest, credentialsResponse) {
    var promises = [];

    if (credentialsRequest !== undefined) {
        console.log ("destroying request");
        promises.push (credentialsRequest.destroy ());
    } else {
        console.log ("no request");
    }

    if (credentialsResponse !== undefined) {
        console.log ("destroying response");
        promises.push (credentialsResponse.destroy ());
    } else {
        console.log ("no response");
    }

    return Parse.Promise.when (promises);
}

// // Example reading from the request query string of an HTTP get request.
// app.get('/test', function(req, res) {
//   // GET http://example.parseapp.com/test?message=hello
//   res.send(req.query.message);
// });

// // Example reading from the request body of an HTTP post request.
// app.post('/test', function(req, res) {
//   // POST http://example.parseapp.com/test (with request body "message=hello")
//   res.send(req.body.message);
// });

// Attach the Express app to Cloud Code.
app.listen();
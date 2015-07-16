# Accredit - A service for getting credentials to services

We use web services to store and present benchmarking results, as well
as to host the benchmarking coordination itself.  To interact with
those services the user, or rather the programs invoked by the user,
require credentials.

We don't want to store those credentials in the program's repository,
because we might want the program itself to be public, apart from
other issues with this approach.  We also would like to avoid having
to ask IT for the credentials.  They are busy enough and this would be
another non-automated hurdle that slows us down.

Accredit solves this problem by using the user's GitHub authentication
to prove their identity and, once proven, provides credentials for
various services.

## Workflow


https://benchmarker.parseapp.com/requestCredentials?service=benchmarker&key=stupidKey&secret=stupidSecret
https://benchmarker.parseapp.com/getCredentials?key=stupidKey&secret=stupidSecret

curl -X GET \
        -H "X-Parse-Application-Id: 7khPUBga9c7L1YryD1se1bp6VRzKKJESc0baS9ES" \
        -H "X-Parse-REST-API-Key: xOHOwaDls0fcuMKLIH0nzaMKclLzCWllwClLej4d" \
        -G \
        https://api.parse.com/1/classes/CredentialsResponse

# Postgres hosting

Our Postgres is currently hosted on an Azure instance.  The credentials are
in [Accredit](../accredit/README.md):

  - `adminPostgres`: The administrator credentials.  Should only be
    required when setting up and converting from Parse.
  - `benchmarkerPostgres`: The credentials with which to insert data.
    Used by `compare.exe`.
  - `postgrestPostgres`: The credentials with which PostgREST runs.
    They only have read access to schema `1`.

# Conversion from Parse

Use the `setupdb.sh` script to convert the database from an export of
Parse.  It takes the path to the export directory.  It'll use the
`Accreditize.exe` command line tools to get all the credentials and
then wipe the whole database before inserting the converted data.

# Deplying PostgREST

[PostgREST](https://github.com/begriffs/postgrest) is a REST front-end
for Postgres.  We're using it from the front-end to access the
database.

It must be deployed like so, with the credentials `postgrestPostgres`:

    docker run -d --name performance-postgrest -p 81:3000 -e DBHOST=<host> -e DBPORT=<port> -e DBNAME=<database> -e DBUSER=<user> -e DBPASS=<password> -e ANONUSER=<user> ubergesundheit/docker-postgrest

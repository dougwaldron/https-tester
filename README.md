# HTTPS & HSTS Settings Tester

A tool for verifying HTTPS and HSTS settings for websites. 

For each website, this app tests and displays whether an HTTP website correctly redirects to HTTPS. A valid redirect will return a 301 status code and a location header that starts with "https://" but is otherwise identical to the original location.

The app then tests whether an HSTS header is set, and if so, it displays the "max-age" value that is set.

## Setup

Add the websites to test in `appsettings.json`. (Don't include "http://" or "https://".) 

To run, execute `dotnet run` at the command line.

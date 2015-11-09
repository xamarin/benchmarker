#!/bin/bash
set -e

if [ "$TRAVIS_PULL_REQUEST" != "false" -o "$TRAVIS_BRANCH" != "master" -o "$TRAVIS_REPO_SLUG" != "xamarin/benchmarker" ]; then
    exit 0;
fi

git config user.name "Travis CI"
git config user.email "bernhard.urban@xamarin.com"

git branch -a
git remote -v

git add -f front-end/build
git add front-end
git commit -am "Deploy to GitHub Pages"

git push --force "https://${GH_TOKEN}@github.com/${TRAVIS_REPO_SLUG}.git" HEAD:gh-pages

#! /bin/bash

if [ $# -ne 1 ]
then
    echo "GitHub token required"
    exit
fi

if [ ! command -v jq &> /dev/null ]
then
    echo "jq required"
    exit
fi

getLatestGitHubRelease() {
    curl "https://api.github.com/repos/$1/releases/latest" | jq -r .tag_name
}

getLatestNugetRelease() {
    curl "https://www.nuget.org/packages/$1/" | grep 'og:title' | sed "s/.*$1 \([^\"]*\).*/\1/"
}

getReleaseDelta() {
    curl https://api.github.com/repos/$1/compare/$2...$3 | jq .files[].filename
}

createRelease() {
    curl -f -H "Authorization: Bearer $GITHUB_TOKEN" -d "{\"tag_name\":\"$2\",\",name\":\"$2\"}" "https://api.github.com/repos/$1/releases"
}

GITHUB_TOKEN=$1
UPSTREAM_GITHUB_RELEASE_TAG=$(getLatestGitHubRelease google/libphonenumber)
DEPLOYED_NUGET_TAG=$(getLatestNugetRelease libphonenumber-csharp)

echo "google/libphonenumber latest release is ${UPSTREAM_GITHUB_RELEASE_TAG}"
echo "libphonenumber-csharp latest release is ${DEPLOYED_NUGET_TAG}"

if [ "$DEPLOYED_NUGET_TAG" = "${UPSTREAM_GITHUB_RELEASE_TAG:1}" ]
then
    echo "versions match"
    exit
fi

mkdir ~/GitHub

(
  cd ~/GitHub
  git clone "https://github.com/twcclegg/libphonenumber-csharp.git"
  cd libphonenumber-csharp
  git checkout main
)
cd ~/GitHub/libphonenumber-csharp/
if [ $(git branch --show-current) != "main" ]
then
    echo "must be on main branch"
    exit
fi

if [ -n "$(git status --porcelain)" ]
then
    echo "working directory is not clean"
    exit
fi

(
  cd ~/GitHub
  git clone "https://github.com/google/libphonenumber.git"
  git checkout "tags/${UPSTREAM_GITHUB_RELEASE_TAG}"
)
cd ~/GitHub/libphonenumber/
PREVIOUS=$(git describe --abbrev=0)

FILES=$(getReleaseDelta google/libphonenumber $PREVIOUS $UPSTREAM_GITHUB_RELEASE_TAG)

if echo $FILES | grep '\.java'
then
   echo "has java"
   exit
fi

if echo $FILES | grep 'proto'
then
   echo "has proto"
   exit
fi

git config --global user.email '<>'
git config --global user.name 'libphonenumber-csharp-bot'

git fetch origin
git reset --hard $UPSTREAM_GITHUB_RELEASE_TAG
rm -rf ../libphonenumber-csharp/resources/*
cp -r resources/* ../libphonenumber-csharp/resources
cd ../libphonenumber-csharp
cd lib
javac DumpLocale.java && java DumpLocale > ../csharp/PhoneNumbers/LocaleData.cs
rm DumpLocale.class
git add -A
git commit -m "$UPSTREAM_GITHUB_RELEASE_TAG"
git push
sleep 15
echo -n "build pending"
sleep 60

createRelease twcclegg/libphonenumber-csharp $UPSTREAM_GITHUB_RELEASE_TAG

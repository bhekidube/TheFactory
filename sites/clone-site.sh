#!/bin/bash

# Clone WhiteLabelAngularSite to a new site
# Usage: ./clone-site.sh <new-site-name>

if [ "$#" -ne 1 ]; then
  echo "Usage: $0 <new-site-name>"
  exit 1
fi

NEW_SITE="$1"
cp -R WhiteLabelAngularSite "$NEW_SITE"
cd "$NEW_SITE"
sed -i '' 's/WhiteLabelAngularSite/'"$NEW_SITE"'/g' angular.json package.json README.md
npm install
cd ..
echo "Site '$NEW_SITE' created from TemplateAngularSite."
echo "Site '$NEW_SITE' created from WhiteLabelAngularSite."

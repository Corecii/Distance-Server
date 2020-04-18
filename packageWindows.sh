#!/bin/bash

mkdir -p $(dirname "$0")/Packaged/ServerWindows/Plugins

for directory in $(dirname "$0")/ServerPlugins/*/; do
	basename = $(basename -- $directory)
	cp $directory/bin/Debug/*.dll $(dirname "$0")/Packaged/ServerWindows/Plugins/
done

cp $(dirname "$0")/ServerBase/bin/Debug/*.dll $(dirname "$0")/Packaged/ServerWindows/

cp -r $(dirname "$0")/ServerUnity/BuildWindows/DistanceServer/* $(dirname "$0")/Packaged/ServerWindows/
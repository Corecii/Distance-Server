#!/bin/bash

mkdir -p $(dirname "$0")/Packaged/Server/Plugins

for directory in $(dirname "$0")/ServerPlugins/*/; do
	basename = $(basename -- $directory)
	cp $directory/bin/Debug/*.dll $(dirname "$0")/Packaged/Server/Plugins/
done

cp $(dirname "$0")/ServerBase/bin/Debug/*.dll $(dirname "$0")/Packaged/Server/

cp -r $(dirname "$0")/ServerUnity/BuildLinux/* $(dirname "$0")/Packaged/Server/
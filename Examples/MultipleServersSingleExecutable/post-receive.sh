chmod +x ./run.sh
chmod +x ./DistanceServer/DistanceServer.x86_64
nohup sh -c 'sleep 15; sh ./run.sh' >&- 2>&- &
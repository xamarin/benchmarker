import sys, subprocess, threading, time

mono = "mono"

kill = threading.Semaphore (0)
exitcode = 0

cmd = ' '.join (sys.argv [1:])

def server ():
    pserver = subprocess.Popen ("%s EventStore.ClusterNode.exe" % (mono), shell=True, stdout=sys.stderr)
    kill.acquire ()
    pserver.kill ()
    pserver.wait ()

def client ():
    time.sleep (10)

    if subprocess.Popen("%s EventStore.TestClient.exe --command ping" % (mono), shell=True, stdout=sys.stderr).wait () == 0:
        start = time.time ()
        s = subprocess.Popen("%s EventStore.TestClient.exe --command '%s'" % (mono, cmd), shell=True, stdout=sys.stderr).wait ()
        end = time.time ()
        if s == 0:
            print (end - start)
        else:
            exitcode = 1
    else:
        exitcode = 2

    kill.release ()

tserver = threading.Thread (target=server)
tclient = threading.Thread (target=client)

tserver.start ()
tclient.start ()

tclient.join ()
tserver.join ()

sys.exit (exitcode)

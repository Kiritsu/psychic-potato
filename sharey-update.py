import requests
import os
import urllib
import zipfile
import shutil
import psutil
import subprocess

def main():
    print('Getting latest build id.')
    buildId = getLatestBuildId()
    print(f'Latest build id is {buildId}.')
    print('Checking if local app is up-to-date.')
    
    if not isUpdateNecessary(buildId):
        print('Local app is up-to-date.')
        return

    print('Downloading latest build\'s artifact.')
    url = getArtifactDownloadUrl(buildId)

    cleanAndShutdownShareY()
    downloadAndUnzipLatestBuild(url, buildId)
    startShareY()

def getLatestBuildId():
    buildsRoute = 'https://dev.azure.com/allanmercou/sharey/_apis/build/builds?api-version=4.1'
    response = requests.get(url = buildsRoute)
    if not response.ok:
        raise Exception('Response from buildsRoute request failed.')
    
    jsonData = response.json()
    return jsonData['value'][0]['id']

def isUpdateNecessary(buildId):
    localBuildId = os.environ.get('SHAREY_BUILD')
    os.environ["SHAREY_BUILD"] = str(buildId)
    
    if localBuildId == None:
        return True

    return int(localBuildId) < buildId

def getArtifactDownloadUrl(buildId):
    artifactRoute = f'https://dev.azure.com/allanmercou/sharey/_apis/build/builds/{buildId}/artifacts?artifactName=sharey&api-version=4.1'
    response = requests.get(url = artifactRoute)
    if not response.ok:
        raise Exception('Response from artifactRoute request failed.')

    jsonData = response.json()
    return jsonData['resource']['downloadUrl']

def cleanAndShutdownShareY():
    processName = 'ShareY'
    for processus in psutil.process_iter():
        if processus.name() == processName:
            processus.kill()
            
    shutil.rmtree('./sharey', ignore_errors = True)

def downloadAndUnzipLatestBuild(downloadUrl, buildId):
    download = urllib.request.urlopen(downloadUrl)
    output = open(f'sharey_build_{buildId}.zip', 'wb')
    output.write(download.read())
    output.close()

    zippedFile = zipfile.ZipFile(f'sharey_build_{buildId}.zip', 'r')
    zippedFile.extractall()
    zippedFile.close()

def startShareY():
    subprocess.call(['nohup', './sharey/ShareY/ShareY', '&'])
    
if __name__ == '__main__':
    main()

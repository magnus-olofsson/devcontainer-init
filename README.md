# DevContainer Init  

A CLI tool for initializing a devcontainer in a project.  
Will connect to preconfigured git repo containing Dockerfiles and devcontainer.json files and list which devcontainer you would like to use.  
Downloads the relevant devcontainer.json file to the correct location, checks whether your machine has the correct docker image, if not the relevant Dockerfile is downloaded and the image is built.
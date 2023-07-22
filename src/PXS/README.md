# This repo is the base repo template for CarbonV2 used for creating other repos

## The .nuget and BuildCore folders are submodules
.nuget from NugetCore repo
BuildCore from BuildCore repo

Your code will live under the Product folder

You will need to rename the BaseRepoTemplate.sln to your project name and then add code projects from/to Product folder in a structure you deem appropriate.

## Updating all submodules
## Cloning a project with submodules will only show the directories, but not the files within
Option 1:
            cd "to submodule directory"
            git submodule init
            git submodule update    

Option 2:
            git clone --recursive https://osgplatforms.visualstudio.com/DefaultCollection/<TeamProject>/_git/<Repo> 

    Other method:
    git submodule update --init --recursive

## If you want to check for new work in a submodule, you can go into the directory and run git fetch and git merge the upstream branch to update the local code.
    git submodule update --recursive --remote


## Reference: 
        https://git-scm.com/book/en/v2/Git-Tools-Submodules

## Mapping of .nuget and BuildCore folders as SubModules
    
    The following will map to the master branch of NugetCore code project to the .nuget folder
    git submodule add -f https://osgplatforms.visualstudio.com/DefaultCollection/<TeamProject>/_git/NugetCore .nuget

    The following will map to the master branch of BuildCore code project
    git submodule add -f https://osgplatforms.visualstudio.com/DefaultCollection/<TeamProject>/_git/BuildCore


## Mapping submodules to different release level
Typical branches for BuildCore and NugetCore are:
    - master
    - development

To map the Development release level of NugetCore to .nuget:
    1) run the "git submodule" command above
    2) git config -f .gitmodules submodule..nuget.branch development
       This will update the shared (remote) branch so everyone will now use the Development release level branch
       If you only want a local update for testing, omit the -f
    3) git submodule update --recursive --remote

## Create new repo
    1) From Web UI
        a. Manage Repositories
        b. Create new Repo
        c. Do not initialize with ReadMe.md
    2) Push from existing Repo branch. Example BaseRepoTemplate:live
    In your local CMD
        a. Cd e:\VSBase\BaseRepoTemplate
        b. Git checkout live
        e:\VSOBase\BaseRepoTemplate>git push https://osgplatforms.visualstudio.com/DefaultCollection/UniversalStore/_git/Create1 +live:master
        
        Result:
            Counting objects: 18, done.
            Delta compression using up to 12 threads.
            Compressing objects: 100% (14/14), done.
            Writing objects: 100% (18/18), 5.47 KiB | 0 bytes/s, done.
            Total 18 (delta 3), reused 6 (delta 0)
            remote: Analyzing objects (18/18) (207 ms)
            remote: Storing pack file and index...  done (944 ms)
            To mshttps://osgplatforms.visualstudio.com/DefaultCollection/UniversalStore/_git/Create1
             * [new branch]      live -> master
        
https://stackoverflow.com/questions/9527999/how-do-i-create-a-new-github-repo-from-a-branch-in-an-existing-repo

## Create a pull request to contribute your changes back into master
Pull requests are the way to move changes from a topic branch back into the master branch.

Click on the **Pull Requests** page in the **CODE** hub, then click "New Pull Request" to create a new pull request from your topic branch to the master branch.

When you are done adding details, click "Create Pull request". Once a pull request is sent, reviewers can see your changes, recommend modifications, or even push follow-up commits.

First time creating a pull request?  [Learn more](https://go.microsoft.com/fwlink/?LinkId=533211&clcid=0x409)

### Congratulations! You've completed the grand tour of the CODE hub!

# Next steps

If you haven't done so yet:
* [Install Visual Studio](https://go.microsoft.com/fwlink/?LinkId=309297&clcid=0x409&slcid=0x409)
* [Install Git](https://git-scm.com/downloads)

Then clone this repo to your local machine to get started with your own project.

Happy coding!
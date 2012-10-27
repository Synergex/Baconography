# Welcome to Baconography
Baconography is an open source reddit client build from the ground up to provide an excellent user experience on Windows 8 regardless of the status of your internet connection.

# Prerequisites for building
*   Visual Studio 2012 (express should work)
*   Windows 8
*   [KitaroDBSDK](http://kitarodb.com/kitarodb-for-winrt/)

# Instructions for building
Open the solution file with Visual Studio 2012. If you dont already have one you will be prompted for a developer license, all it takes is a Microsoft live account.

Once its opened we need to restore the nuget packages. the easiest way to do that is to right click on the project and goto "Manage NuGet Packages...". There should be a button to restore the pacakges at the top of that dialog.

Now that everything is set up, you can build, deploy, and debug the solution

# What to do if things break
*   Make an issue for it here on github
*   Make a posting on [the Baconography subreddit](http://reddit.com/r/baconography)
*   Send messages on reddit to either hippiehunter or madkatalpha

# Things we can do right now
*   Store Links and Comments for offline viewing [Using KitaroDB](http://www.kitarodb.com)
*   Infinite scrolling everywhere
*   Support for browsing reddit while not logged in
*   Support for multiple saved account credentials for rapid switching
*   Voting, saving, reporting and all the things you expect to do to a link or comment
*   Integration with Windows Search, Share and File Picker (pick photos directly from reddit links)
*   Ability to pin subreddits to the start screen
*   Integrated markdown preview when creating a reply
*   Launch links in the app or open them in a browser
*   Optional filtering of NSFW content

# Things left on the Todo list
*   Get it in the Windows 8 Store
*   Use the imgur api (and the other domains supported by RES) to download and display images/albums directly
*   Use NReadability to generate offline versions of (non image) links
*   Add drop down tick for 'sort by' in RedditView and CommentsView
*   If nothing in offline cache when we're in offline mode show a message letting the user know there isnt anything there
*   Implement messaging
*   Add support for a lockscreen indicator to show messages/replys 
*   Fix layout for 1/4 and 3/4 snapped view
*   Multi-Reddit selector
*   Add support for Posting
*   Add in RES style keyboard shortcuts

# License
Copyright (c) 2012 Synergex International Corporation

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in  the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
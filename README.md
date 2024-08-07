This project aims to help bring Challonge and Google Sheets together to allow for automation of data between the two applications. This project is also to give me some more Learning experience with Blazor after the endd of my Honours Project for uni. I created a Blazor App as part of my Honours Project and want to continue learning about it and also further develop my C# Knowladge. 

# Project Overiew

The goals for this is to use the Challonge API within a Web App to allow the easy addition of Participants and easy check-in when the arrive. Participents will also be able to be added in the future on the main check-in page. 

Currently you can add a link to the Challonge Tournement and set the Check-in of a partitipent and it will show the check-in time for them. This needs to be updated so that the check-in can be undone.

The brackets can be displayed, though they are a still a bit messy for the format and need to be cleaned up.

## Note on the current code 07/08/24

Just thought I'd mention that the current state of the code is kinda messy still and needs a good clean up, this is something I'm going to set time aside for. There's a few intances of repeated code where i want to add helper classes so that they can be called when required. Please keep that in mind if you are having a look through. Comments will also get added as well because it annoys me when I look through other peoples stuff and there is no indication to what is actually happening in the code, I always just end up doing it when doing a code clean up.

# Feature Requests

If you have any requests, add a new Issue and set the Lable to Feature request and give an outline of what you want added to the application or if you think something should be handled differently.

# TODO

| Feature | Description | Done |
| ----------- | ----------- | ----------- | 
| List Challonge Players | Have a list of players displayed from the Challonge API | <ul><li>[x] </li></ul>|
| Enable Check-in | Allow participents to be checked in via app and send to CHallonge API | <ul><li>[x] </li></ul>|
| Undo Check-in | Allow parctipents to be un-checked-in(?) in the event of an accidental check-in | <ul><li>[ ] </li></ul>|
| Add New Participent | Allow participancts to be added on site and updated on Challonge | <ul><li>[ ] </li></ul>
| Add Region Info | Add region selection for players and have the info taken or saved to Sheets | <ul><li>[ ] </li></ul>
| Add Brackets | Add brackekts with data pulled from Challonge | <ul><li>[x] </li></ul>|
| Push Modified Data From brackets | Allow data to be updated from the web app to Challonge | <ul><li>[ ] </li></ul>|
| Google Sheets Intergration | Setup the API to Google Sheets | <ul><li>[ ] </li></ul>|
| Display Leaderboards | Display leaderboard formatted from Google Sheets | <ul><li>[ ] </li></ul>|
| Add new Player to Leaderboard | If a user isn't found on the leaderboard table Add them (Note: Might be worth adding the Challonge ID in here too that is used on the API so that it may be possible to check for any name changes)| <ul><li>[ ] </li></ul>|
| Auto update Leaderboards | Update leaderboards based on match Results | <ul><li>[ ] </li></ul>|
| Add Local Settings | This has already been done for Challonge related stuff, but google sheets info needs updated on there| <ul><li>[ ] </li></ul>|
| Add Offline Mode | Add an offline mode so that in the event that there is no internet connection, data from the tournement can be stored and then pushed up once there is a connection | <ul><li>[ ] </li></ul>







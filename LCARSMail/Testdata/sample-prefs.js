// Synthetic Thunderbird-style prefs for offline parser verification (fake hosts only).
user_pref("mail.accountmanager.accounts", "account1,account2,local,feed,nntpacct");

user_pref("mail.account.account1.identities", "id1");
user_pref("mail.account.account1.server", "server1");

user_pref("mail.server.server1.type", "imap");
user_pref("mail.server.server1.hostname", "imap.example.lcars.test");
user_pref("mail.server.server1.port", 993);
user_pref("mail.server.server1.socketType", 3);
user_pref("mail.server.server1.userName", "crew@example.lcars.test");

user_pref("mail.identity.id1.fullname", "LCARS Mail Fixture");
user_pref("mail.identity.id1.useremail", "crew@example.lcars.test");

user_pref("mail.account.account2.identities", "id2");
user_pref("mail.account.account2.server", "server2");
user_pref("mail.server.server2.type", "imap");
user_pref("mail.server.server2.hostname", "imap-starttls.example.lcars.test");
user_pref("mail.server.server2.port", 143);
user_pref("mail.server.server2.socketType", 2);
user_pref("mail.server.server2.userName", "starttls@example.lcars.test");
user_pref("mail.identity.id2.fullname", "STARTTLS Fixture");
user_pref("mail.identity.id2.useremail", "starttls@example.lcars.test");

user_pref("mail.account.local.identities", "idlocal");
user_pref("mail.account.local.server", "serverlocal");
user_pref("mail.server.serverlocal.type", "none");
user_pref("mail.server.serverlocal.userName", "nobody");

user_pref("mail.account.feed.identities", "idfeed");
user_pref("mail.account.feed.server", "serverfeed");
user_pref("mail.server.serverfeed.type", "rss");
user_pref("mail.server.serverfeed.hostname", "feed.example.lcars.test");

user_pref("mail.account.nntpacct.identities", "idnntp");
user_pref("mail.account.nntpacct.server", "servernntp");
user_pref("mail.server.servernntp.type", "nntp");
user_pref("mail.server.servernntp.hostname", "news.example.lcars.test");

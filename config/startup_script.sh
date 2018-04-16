#!/bin/bash
sh -c 'echo "deb [arch=amd64] https://apt-mo.trafficmanager.net/repos/dotnet-release/ trusty main" > /etc/apt/sources.list.d/dotnetdev.list'
apt-key adv --keyserver apt-mo.trafficmanager.net --recv-keys 417A0893
apt-get update
apt-get install dotnet-dev-1.0.0-preview4-004233  -y
apt-get install git -y
apt-get install nginx -y
apt-get install supervisor -y
apt-get install libgdiplus -y
ln -s /usr/lib/libgdiplus.so /usr/lib/gdiplus.dll
apt-get install npm -y
npm install -g bower
ufw enable -y
ufw allow 22/tcp
ufw allow 80/tcp
mkdir /var/www
mkdir /amazonwebapp
chmod 777 -R amazonwebapp
export HOME="$(cd "$HOME" ; pwd)"
git clone https://github.com/dbartek/aws-net-sample.git
cd /aws-net-sample/src/AmazonWebApp
dotnet restore
dotnet publish
cp -r /aws-net-sample/src/AmazonWebApp/bin/Debug/netcoreapp1.0/publish /var/www/amazonwebapp
echo '{
  "AWS_ACCESS_KEY_ID": "",
  "AWS_SECRET_ACCESS_KEY": "",
  "AWS_BUCKET_NAME": "",
  "AWS_BUCKET_URL": "",
  "AWS_SQS_QUEUE_URL": ""
}' > /var/www/amazonwebapp/awssettings.json
/bin/cp /aws-net-sample/config/nginx.conf /etc/nginx/sites-available/default
/bin/cp /aws-net-sample/config/supervisor.conf /etc/supervisor/conf.d/amazonwebapp.conf
service supervisor restart
service nginx restart
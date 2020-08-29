#!/bin/bash
set -e

# process all the supplied or default site configurations
SITES=( /etc/apache2/sites-available/* )

if [[ ${SITES[@]} ]]; then
    # disable all the currently enabled sites
    find /etc/apache2/sites-enabled/ -type l -exec rm -f "{}" \;
    # enable all the supplied sites
    for ITEM in "${SITES[@]}";
    do
        FILE=$(basename -- "$ITEM")
        SITE="${FILE%.*}"
        a2ensite $SITE.conf
        mkdir -p    /var/www/$SITE \
                    /var/log/apache2/$SITE
    done
    echo "Serving: $SITES"
fi

# delete the existing PID file if any as apache is reportedly picky about that
rm -f $(. /etc/apache2/envvars && echo $APACHE_PID_FILE)

exec apachectl "$@"

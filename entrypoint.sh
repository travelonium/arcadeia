__='
   Copyright © 2024 Travelonium AB

   This file is part of Arcadeia.

   Arcadeia is free software: you can redistribute it and/or modify
   it under the terms of the GNU Affero General Public License as published
   by the Free Software Foundation, either version 3 of the License, or
   (at your option) any later version.

   Arcadeia is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
   GNU Affero General Public License for more details.

   You should have received a copy of the GNU Affero General Public License
   along with Arcadeia. If not, see <https://www.gnu.org/licenses/>.

'

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

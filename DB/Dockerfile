FROM mcr.microsoft.com/mssql/server:2022-latest

USER root

COPY Ingenieria_Purrujas_BD.sql /docker-entrypoint-initdb.d/Ingenieria_Purrujas_BD.sql
COPY docker-entrypoint.sh /usr/local/bin/docker-entrypoint.sh
COPY sqlserver-healthcheck.sh /usr/local/bin/sqlserver-healthcheck.sh

RUN sed -i 's/\r$//' /usr/local/bin/docker-entrypoint.sh /usr/local/bin/sqlserver-healthcheck.sh \
    && chmod +x /usr/local/bin/docker-entrypoint.sh /usr/local/bin/sqlserver-healthcheck.sh \
    && chown -R mssql:root /docker-entrypoint-initdb.d \
    && chown mssql:root /usr/local/bin/docker-entrypoint.sh /usr/local/bin/sqlserver-healthcheck.sh

USER mssql

EXPOSE 1435

ENTRYPOINT ["/usr/local/bin/docker-entrypoint.sh"]

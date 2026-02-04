FROM php:8.2-apache

RUN apt-get update && apt-get install -y \
    libcurl4-openssl-dev \
    pkg-config \
    libssl-dev \
    && docker-php-ext-install curl \
    && a2enmod rewrite

COPY . /var/www/html/

RUN chown -R www-data:www-data /var/www/html \
    && chmod 1777 /tmp \
    && mkdir -p /tmp/php_sessions \
    && chown -R www-data:www-data /tmp/php_sessions \
    && chmod 1777 /tmp/php_sessions \
    && mkdir -p /var/lib/php/sessions \
    && chown -R www-data:www-data /var/lib/php/sessions \
    && chmod 1777 /var/lib/php/sessions

EXPOSE 80

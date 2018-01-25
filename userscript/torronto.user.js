// ==UserScript==
// @name         Kinopoisk-Torronto
// @namespace    http://torronto.evilbloodydemon.ru/
// @version      0.1
// @description  Кнопка перехода на торронту для кинопоиска
// @author       Igor Fomin
// @match        http://www.kinopoisk.ru/film/*
// @grant        none
// ==/UserScript==

$(function() {
    $('h1').append('&nbsp;<button class="torronto-button" style="cursor: pointer">Torronto</button>');
    $('.torronto-button').on('click', function(){
        window.location.href = 'http://torronto.evilbloodydemon.ru/#!/movies?open_single=1&kinopoisk_id=' + FILM_ID;
    });
});

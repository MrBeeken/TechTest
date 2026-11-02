/* ==========================================================================
 *  Menu Explorer — menu-search.js
 * ==========================================================================
 *  Role    Progressive enhancement: filter the rendered cards as the user types,
 *          and keep the live result counter honest.
 *  Layer   Client asset. The page is fully usable without it.
 *  Debug   The toolbar is rendered hidden and revealed here, so a search box that
 *          appears but does nothing means this file failed to load. The counter's
 *          total comes from data-total-items on the grid, never from the number
 *          of cards, or a category-filtered page would report "of 5".
 * ========================================================================== */
(function () {
    'use strict';

    const searchInput = document.querySelector('[data-menu-search]');
    const toolbar = document.querySelector('[data-menu-toolbar]');
    const grid = document.querySelector('[data-menu-grid]');
    const emptyState = document.querySelector('[data-search-empty]');
    const resultCounter = document.querySelector('[data-result-count]');

    // Not on the menu page (or the markup changed): do nothing, and leave the
    // toolbar hidden so no inert control is ever focusable.
    if (!searchInput || !grid || !resultCounter) {
        return;
    }

    const menuCards = Array.prototype.slice.call(grid.querySelectorAll('[data-menu-card]'));
    if (menuCards.length === 0) {
        return;
    }

    // The server tells us how many dishes exist across the *whole* menu; the
    // cards on this page may be a category-filtered subset of that.
    const parsedTotal = Number.parseInt(grid.getAttribute('data-total-items'), 10);
    const menuTotal = Number.isFinite(parsedTotal) && parsedTotal > 0 ? parsedTotal : menuCards.length;

    // Pre-compute one searchable string per card so typing stays O(n) with no
    // repeated DOM reads.
    const haystacks = menuCards.map(function (card) {
        return normalize([
            card.getAttribute('data-name'),
            card.getAttribute('data-description'),
            card.getAttribute('data-category')
        ].join(' '));
    });

    // Lowercase and strip diacritics, so "crème" and "creme" both match.
    function normalize(value) {
        return (value || '')
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '')
            .toLowerCase()
            .trim();
    }

    function pluralise(count) {
        return count === 1 ? 'dish' : 'dishes';
    }

    function apply() {
        const term = normalize(searchInput.value);
        let visibleCount = 0;

        for (let index = 0; index < menuCards.length; index++) {
            const matches = term === '' || haystacks[index].indexOf(term) !== -1;
            menuCards[index].hidden = !matches;
            if (matches) {
                visibleCount++;
            }
        }

        const isSearching = term !== '';

        if (emptyState) {
            emptyState.hidden = !(isSearching && visibleCount === 0);
        }

        resultCounter.textContent = visibleCount === 0
            ? 'No dishes match your search'
            : 'Showing ' + visibleCount + ' of ' + menuTotal + ' ' + pluralise(menuTotal);
    }

    function clearSearch() {
        searchInput.value = '';
        apply();
        searchInput.focus();
    }

    // Only now is the search box functional, so only now is it worth showing.
    if (toolbar) {
        toolbar.hidden = false;
    }

    apply();

    searchInput.addEventListener('input', apply);

    // Fired by the native clear (×) control in some browsers.
    searchInput.addEventListener('search', apply);

    searchInput.addEventListener('keydown', function (event) {
        if (event.key === 'Escape' && searchInput.value !== '') {
            event.preventDefault();
            clearSearch();
        }
    });

    if (emptyState) {
        const clearButton = emptyState.querySelector('[data-search-clear]');
        if (clearButton) {
            clearButton.addEventListener('click', clearSearch);
        }
    }
})();
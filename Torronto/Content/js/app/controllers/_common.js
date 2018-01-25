function updateSocialShare(title, href) {
    if (!!window.YaShareInstance) {
        window.YaShareInstance.updateShareLink(href || window.location.href, title || 'Torronto');
    }
}
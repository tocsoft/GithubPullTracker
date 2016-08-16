
window.marked.setOptions({
    gfm: true,
    tables: true,
    breaks: false,
    pedantic: false,
    sanitize: true,
    smartLists: true,
    smartypants: false
});

function markdown(markdown) {

    var re_issuesWithRepo = /(\s|^)([a-zA-Z0-9][a-zA-Z0-9_]{1,14}\/[a-zA-Z0-9-_.]+)#(\d+)(\s|$)/gm;
    var subst_issuesWithRepo = '$1[$2#$3](https://github.com/$2/issues/$3)$4';

    var re_issues = /(\s|^)#(\d+)(\s|$)/gm;
    var subst_issues = '$1[#$2](https://github.com/' + window.repocontext + '/issues/$2)$3';

    markdown = markdown
        .replace(re_issuesWithRepo, subst_issuesWithRepo)
        .replace(re_issues, subst_issues)
    ;


    var html = marked(markdown);

    var re_links = /(<a)( href=")/gm;
    var subst_links = '$1 target=\'_blank\'$2';

    return html.replace(re_links, subst_links);
}
function inversePatch(patch) {
    for (var i = 0; i < patch.length; i++) {
        var p = patch[i];
        for (var j = 0; j < p.hunks.length; j++) {
            var h = p.hunks[j];
            for (var k = 0; k < h.lines.length; k++) {
                var line = h.lines[k];

                if (line[0] == '-') {
                    line = '+' + line.substring(1);
                } else if (line[0] == '+') {
                    line = '-' + line.substring(1);
                }

                h.lines[k] = line;
            }

            var t = h.newLines;
            h.newLines = h.oldLines;
            h.oldLines = t;

            var t = h.newStart;
            h.newStart = h.oldStart;
            h.oldStart = t;
        }
    }
    return patch;
}

function patchToHtml(patch) {

    if (typeof patch === 'string') {
        patch = JsDiff.parsePatch(patch);
    }
    var html = "<table>";
    for (var i = 0; i < patch.length; i++) {
        var p = patch[i];
        for (var j = 0; j < p.hunks.length; j++) {
            var h = p.hunks[j];
            var newLine = h.newStart -1;
            var oldLine = h.oldStart -1;
            for (var k = 0; k < h.lines.length; k++) {

                var line = h.lines[k];

                var cls = '';
                if (line[0] == '-') {
                    cls = 'removed';
                } else if (line[0] == '+') {
                    cls = 'added';
                }
                html += "<tr class='" + cls + "'>";

                html += "<td class='src'>";
                if (line[0] == ' ' || line[0] == '-') {
                    oldLine++;
                    html += oldLine;
                }
                html += "</td>";
                html += "<td class='target'>";
                if (line[0] == ' ' || line[0] == '+') {
                    newLine++;
                    html += newLine;
                }
                html += "</td>";

                html += "<td class='txt'>";
                html += line.substring(1);
                html += "</td>";

                html += "</tr>";
            }
        }
    }

    html += "</table>";
    return html;

}

$(document).on('contentReloaded', function (e) {
    $('.apply-md', e.target).each(function () {
        var $this = $(this);

        var md = $this.html();
        $this.html(markdown(md));
        $this.removeClass('apply-md');
    });

    $('.apply-patch', e.target).each(function () {
        var $this = $(this);

        var md = $this.html();
        $this.html(patchToHtml(md));
        $this.removeClass('apply-patch');
    });
    $('.timeago', e.target).timeago();
    
});

$(document).trigger('contentReloaded');


(function () {

    var ddl = $('.navbar .user ul').hide();
    var avatar = $('.navbar .user .avatar');
$('.navbar .user .avatar').click(function (e) {
    
    ddl.toggle();
    e.preventDefault();
});


$(document).click(function (e) {
    if (e.target != avatar[0] && e.target.parentNode != avatar[0]) {
        ddl.hide();
    }
    //console.log('clicked off');
});
})();
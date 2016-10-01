
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

function mapPatch(patch)
{
    var results = {
        oldLines: {},
        newLines: {},
        patchLines: {}
    };

    var patchLine = 0;
    for (var i = 0; i < patch.length; i++) {
        var p = patch[i];
        for (var j = 0; j < p.hunks.length; j++) {
            patchLine++
            var h = p.hunks[j];
            var newLine = h.newStart - 1;
            var oldLine = h.oldStart - 1;
            for (var k = 0; k < h.lines.length; k++) {
                patchLine++;
                var line = h.lines[k];

                var det = results.patchLines[patchLine] = {}
                
                if (line[0] == ' ' || line[0] == '-') {
                    oldLine++;
                    
                    det.oldLine = oldLine;
                    results.oldLines[oldLine] = patchLine;
                }
                if (line[0] == ' ' || line[0] == '+') {
                    newLine++;

                    det.newLine = newLine;
                    results.newLines[newLine] = patchLine;
                }
               
            }
        }
    }
    return results;
}
function escapeRegExp(str) {
    return str.replace(/[\-\[\]\/\{\}\(\)\*\+\?\.\\\^\$\|]/g, "\\$&");
}
String.prototype.replaceAll = function (search, replacement) {
    var target = this;
    return target.replace(new RegExp(escapeRegExp(search), 'g'), replacement);
};

function inversePatch(patchs) {
    if (!patchs) {
        return [];
    }

    var patchsResult = []
    for (var i = 0; i < patchs.length; i++) {
        var p = patchs[i];
        var pr = { hunks: [] };
        patchsResult.push(pr);
        for (var j = 0; j < p.hunks.length; j++) {
            var h = p.hunks[j];
            var hr = { lines : [] };
            pr.hunks.push(hr);

            for (var k = 0; k < h.lines.length; k++) {
                var line = h.lines[k];

                if (line[0] == '-') {
                    line = '+' + line.substring(1);
                } else if (line[0] == '+') {
                    line = '-' + line.substring(1);
                }
                hr.lines.push(line)
            }

            hr.oldLines = h.newLines;
            hr.newLines = h.oldLines;

            hr.oldStart = h.newStart;
            hr.newStart = h.oldStart;
        }
    }

    return patchsResult;
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

$(function () {
    $('#file-list li > span').click(function () {
        $(this).parent().find('> ul').toggle();
    });


    //lets inject  a repo search box

    $('#repoList').each(function () {


        var searcher = $(".searcher input")


        var repoList = $('[data-repo]', this);
        var ownerList = $('h3[data-owner]', this);
        var allItems = $('h3[data-owner], a', this);
        var searchTo;
        searcher.on("keyup keypress change", function (e) {
            clearTimeout(searchTo);
            searchTo = setTimeout(function () {
                var toFind = searcher.val();
              
                if (toFind === "") {
                    allItems.show();
                } else {
                    var foundOwners = [];
                    var terms = toFind.split(' ');
                        repoList.each(function () {
                            var $this = $(this);
                            var repo = $this.attr('data-repo');
                            var owner  = $this.attr('data-owner') || '';
                            var show = true;
                            for (var i in terms) {
                                var term = terms[i];
                                

                                if (!(repo.indexOf(term) > -1 || owner.indexOf(term) > -1)) {
                                    show = false;
                                }
                            };
                           if(show){
                                $this.parent().show();
                                foundOwners.push($this.attr('data-owner'));
                            } else {
                                $this.parent().hide();
                            }
                        });
                    ownerList.each(function () {
                        var $this = $(this);
                        var n = $this.attr('data-owner');
                        if (foundOwners.indexOf(n) > -1) {
                            $this.show();
                        } else {
                            $this.hide();
                        }
                    })
                }
            }, 100);
        })
    });


    (function ($) {
        $.fn.contrastingText = function () {
            this.each(function () {
                var el = $(this),
                    transparent;
                transparent = function (c) {
                    var m = c.match(/[0-9]+/g);
                    if (m !== null) {
                        return !!m[3];
                    }
                    else return false;
                };
                while (transparent(el.css('background-color'))) {
                    el = el.parent();
                }
                parts = el.css('background-color').match(/[0-9]+/g);
                var lightBackground = !!Math.round(
                    (
                        parseInt(parts[0], 10) + // red
                        parseInt(parts[1], 10) + // green
                        parseInt(parts[2], 10) // blue
                    ) / 765 // 255 * 3, so that we avg, then normalise to 1
                );
                if (lightBackground) {
                    el.css('color', 'black');
                } else {
                    el.css('color', 'white');
                }
            });
            return this;
        };
    }(jQuery));

    $('.label').contrastingText();
    $('g-emoji').each(function () {
        var $this = $(this);
        var src = $this.attr('fallback-src');
        var alias = $this.attr('alias');
        $this.html("<img src='" + src + "' width='20' alt=':" + alias + ":'/>");
    })
})


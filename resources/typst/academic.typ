// Pandoc Typst template for the "academic" document style.
// Numeric and font placeholders are replaced by TypstTemplateRenderer before Pandoc runs.

#set document(
  title: [$if(title)$$title$$endif$],
  author: "$if(author)$$for(author)$$if(author.name)$$author.name$$else$$author$$endif$$sep$, $endfor$$endif$",
  keywords: ($if(keywords)$$for(keywords)$"$keywords$"$sep$,$endfor$$endif$),
)

#set page(
  paper: "@@PAPER@@",
  margin: (
    top: @@MARGIN_TOP@@mm,
    right: @@MARGIN_RIGHT@@mm,
    bottom: @@MARGIN_BOTTOM@@mm,
    left: @@MARGIN_LEFT@@mm,
  ),
  numbering: none,
)

#set text(
  font: (@@FONT_LIST@@),
  size: @@FONT_SIZE@@pt,
  lang: "$if(lang)$$lang$$else$zh$endif$",
  region: "$if(region)$$region$$else$CN$endif$",
)

#set par(
  justify: true,
  leading: @@LEADING@@em,
  spacing: @@PAR_SPACING@@pt,
)

#set heading(numbering: none)
#show heading: set text(weight: "bold")
#show heading: set block(above: @@HEADING_BEFORE@@pt, below: @@HEADING_AFTER@@pt)
#show heading.where(level: 1): set text(size: @@H1_SIZE@@em, fill: rgb("#000000"))
#show heading.where(level: 2): set text(size: @@H2_SIZE@@em, fill: rgb("#000000"))
#show heading.where(level: 3): set text(size: @@H3_SIZE@@em, fill: rgb("#000000"))
#show heading.where(level: 4): set text(size: @@H4_SIZE@@em, fill: rgb("#000000"))
#show heading.where(level: 5): set text(size: @@H5_SIZE@@em, fill: rgb("#000000"))
#show heading.where(level: 6): set text(size: @@H6_SIZE@@em, fill: rgb("#000000"))

#show link: set text(fill: rgb("#0000EE"))
#show raw: set text(font: (@@MONO_FONT_LIST@@), size: 0.88em)
#show raw.where(block: true): set block(
  fill: rgb("#F5F5F5"),
  inset: 8pt,
  radius: 3pt,
  width: 100%,
)
#show table: set table(stroke: 0.5pt + rgb("#000000"), inset: 6pt)
#show table.cell.where(y: 0): set table.cell(fill: rgb("#E7E6E6"))

$if(title)$
#align(center)[
  #text(size: @@TITLE_SIZE@@em, weight: "bold")[$title$]
  $if(subtitle)$
  #v(0.25em)
  #text(size: 1.2em, fill: rgb("#000000"))[$subtitle$]
  $endif$
  $if(author)$
  #v(0.8em)
  #text(size: 1em)[$for(author)$$if(author.name)$$author.name$$else$$author$$endif$$sep$, $endfor$]
  $endif$
  $if(date)$
  #v(0.3em)
  #text(size: 1em, fill: rgb("#666666"))[$date$]
  $endif$
]
#pagebreak()
$endif$

$if(toc)$
#outline(title: [目录], depth: 3)
#pagebreak()
$endif$

#set page(
@@PAGE_DECORATIONS@@
)
#counter(page).update(1)

$body$

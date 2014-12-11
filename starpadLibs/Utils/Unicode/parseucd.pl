#!/usr/bin/perl
open(UCD, "<UnicodeData.txt") || die "Can't read UnicodeData.txt: $!";
print "using System;\n";
print "using System.Collections.Generic;\n";
print "\n";
print "/*
REFER TO THE SECOND LINK (NamesList.txt) TO PICK A CHARACTER. Really!
It has comments and information as to what the symbols are supposed to
represent.

This file contains strings extracted from http://www.unicode.org/Public/UNIDATA/UnicodeData.txt by running parseucd.pl.
However, refer to http://www.unicode.org/Public/UNIDATA/NamesList.txt for more
information on each character, such as description, alternate names, synonyms,
usage, etc. (And, in general, refer to that whole site.)

Where there's a comment saying \"consider avoiding\", that's because some
characters such as superscript numbers and letters are better represented in
Exprs in other ways. However, it's automatically generated, so it might be
wrong. See NamesList.txt for more information in such cases.

Unicode version 4.1 was current at the time of writing, though this file only
contains the low 16 bit unicode characters as that's all C# seems to support.

COPYRIGHT AND PERMISSION NOTICE

Copyright © 1991-2005 Unicode, Inc. All rights reserved. Distributed under the
Terms of Use in http://www.unicode.org/copyright.html.

Permission is hereby granted, free of charge, to any person obtaining a copy of
the Unicode data files and any associated documentation (the \"Data Files\") or
Unicode software and any associated documentation (the \"Software\") to deal in
the Data Files or Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, and/or sell copies
of the Data Files or Software, and to permit persons to whom the Data Files or
Software are furnished to do so, provided that (a) the above copyright
notice(s) and this permission notice appear with all copies of the Data Files
or Software, (b) both the above copyright notice(s) and this permission notice
appear in associated documentation, and (c) there is clear notice in each
modified Data File or in the Software as well as in the documentation
associated with the Data File(s) or Software that the data or software has been
modified.

THE DATA FILES AND SOFTWARE ARE PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY
KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT OF THIRD
PARTY RIGHTS. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR HOLDERS INCLUDED IN
THIS NOTICE BE LIABLE FOR ANY CLAIM, OR ANY SPECIAL INDIRECT OR CONSEQUENTIAL
DAMAGES, OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS,
WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING
OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THE DATA FILES OR
SOFTWARE.

Except as contained in this notice, the name of a copyright holder shall not be
used in advertising or otherwise to promote the sale, use or other dealings in
these Data Files or Software without prior written authorization of the
copyright holder.
*/

";
print "namespace starPadSDK.MathExpr {\n";
print "\tpublic class Unicode {\n";

while(<UCD>) {
  chomp;
  split /;/;
  next if $_[1] =~ /\<.+\>/;
  next if !($_[0] =~ /^....$/);
  $name = &nametoid($_[1]);
  $nameof{$name} = lc $_[1];
  push @names, $name;
  push @{$firstchar{substr($name,0,1)}}, $name;
  $values{$name} = "0x$_[0]";
  if($_[10] || $_[5]) {
    $cmnt = "/// <summary>";
    $cmnt .= " (This is a composition; consider avoiding.)" if $_[5];
    $cmnt .= " Formerly $_[10]." if $_[10];
    $cmnt .= " </summary>";
    $comments{$name} = $cmnt;
  }
}

foreach $let (sort keys %firstchar) {
  print "\t\tpublic class $let {\n";
  foreach $name (sort @{$firstchar{$let}}) {
    print "\t\t\t", $comments{$name}, "\n" if(defined $comments{$name});
    print "\t\t\tpublic const char $name = (char)$values{$name};\n";
  }
  print "\t\t}\n";
}

print "\n";

print "\t\tprivate static Dictionary<char,string> _charNames;
\t\tstatic Unicode() {
\t\t\t_charNames = new Dictionary<char,string>();\n";
foreach $name (@names) {
  print "\t\t\t_charNames[(char)$values{$name}] = \"$nameof{$name}\";\n";
}
print "\t\t}
\t\tpublic static string NameOf(char c) { return _charNames[c]; }\n";

print "\t}\n";
print "}\n";

sub nametoid {
  my ($name) = @_;
  $name =~ tr/ -/__/;
  return $name;
}

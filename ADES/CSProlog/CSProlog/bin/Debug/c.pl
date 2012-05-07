
path(X,Y) :- path(X,Y,[X],_).

path(X,X,A,A).
path(X,Y,A,R) :- 
	X\==Y, 
	dir_edge(X,Z), 
	absent(Z,A), 
	path(Z,Y,[Z|A],R).

absent(X,[Y|Z]):-X \= Y, absent(X,Z).
absent(_,[]).



"0.9::dir_edge(1,2)".
"0.8::dir_edge(2,3)".
"0.6::dir_edge(3,4)".
"0.7::dir_edge(1,6)".
"0.5::dir_edge(2,6)".
"0.4::dir_edge(6,5)".
"0.7::dir_edge(5,3)".
"0.2::dir_edge(5,4)".
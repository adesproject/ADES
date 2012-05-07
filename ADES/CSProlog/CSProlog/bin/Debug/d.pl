
path(X,Y,[X|NL]) :- pathX(X,Y,L), reverse(L,[],NL), notvisited(X,NL).
pathX(X,Y,L) :- pathX(X,Y,_,L).
pathX(X,X,A,A).
pathX(X,Y,A,R) :- 
	X\==Y, 
	edge(X,Z), 
	notvisited(Z,A), 
	pathX(Z,Y,[Z|A],R).

notvisited(X,[Y|Z]):- X \= Y, notvisited(X,Z).
notvisited(_,[]).

reverse([X|Y],Z,W) :- reverse(Y,[X|Z],W).
reverse([],X,X).

"0.9::edge(1,3)".
"0.5::edge(1,2)".
"0.3::-20::edge(2,4)".
"0.8::edge(2,5)".
"0.8::edge(3,4)".
"0.7::edge(4,1)".
"0.6::edge(4,5)".


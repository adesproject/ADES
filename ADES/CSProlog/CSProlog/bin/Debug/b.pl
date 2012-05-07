
violation(X) :- noturn(X), steer(X), lanedept(X).

noturn(left).
noturn(right).


lanedept(right).
lanedept(left).



steer(right).
steer(left).






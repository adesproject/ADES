

violation(X) :- noturn(X), steer(X), lanedept(X).


"0.85::noturn(left)".
"0.8::steer(left)".


"0.1::noturn(right)".
"0.2::steer(right)".
"0.5::lanedept(right)".



"0.9::lanedept(left)".
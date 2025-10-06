CREATE EVENT clear_tokens
	ON SCHEDULE EVERY 1 DAY
    DO CALL clean_tokens();

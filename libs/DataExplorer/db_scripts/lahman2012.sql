x Batting 
x Fielding
x Master
x Pitching
  Teams
x TeamsFranchises
x AllStarFull
x Hall of Fame
x BattingPost
x PitchingPost
x FieldingOF
x Salaries
x AwardsPlayers
x AwardsSharePlayers.xls
x Appearances
x SchoolsPlayers

dim_players
dim_teamFranchise
dim_schools 
dim_year 



create table baseball.fact_player_stats  
(id int, player_id int,franch_id int, year_id int, school_id int, stint int, g int, 
g_batting int, ab int, r int, h int, b2 int, b3 int, hr int, rbi int, sb int, 
cs int, bb int, so int, ibb int, hbp int, sh int, sf int, gidp int, w int, 
l int, gs int, cg int, sho int, sv int, ipouts int, ph int, er int, 
phr int, pbb int, pso int, baopp int, era int,pibb int, wp int, phbp int, 
bk int, bfp int,gf int, pr int,  psf int, fg int, 
fgs int, innouts int, po int, a int, e int, dp int, pb int, fsb int,
fcs int, zr int, gamenum int, allstargp int, ballots int, needed int,votes int, 
playoff_g int,playoff_ab int, playoff_r int, playoff_h int, 
playoff_2b int, playoff_3b int, playoff_hr int, playoff_rbi int, playoff_sb int, 
playoff_cs int, playoff_bb int, playoff_so int, playoff_ibb int, playoff_hbp int, 
playoff_sh int, playoff_sf int, playoff_gidp int, pitchplayoff_w int, 
pitchplayoff_l int, pitchplayoff_g int, pitchplayoff_gs int, pitchplayoff_cg int, 
pitchplayoff_sho int, pitchplayoff_sv int, pitchplayoff_ipouts int, 
pitchplayoff_h int,pitchplayoff_er int, pitchplayoff_hr int, pitchplayoff_bb int, 
pitchplayoff_so int, pitchplayoff_baopp int, pitchplayoff_era int, pitchplayoff_ibb int, 
pitchplayoff_wp int,pitchplayoff_hbp int, pitchplayoff_bk int, pitchplayoff_BFP int, 
pitchplayoff_gf int, pitchplayoff_r int, pitchplayoff_sh int, pitchplayoff_sf int, 
pitchplayoff_gidp int, glf int, grf int, gcf int, salary float, award int, 
fieldplayoffs_g int, fieldplayoffs_gs int, fieldplayoffs_innouts int, 
fieldplayoffs_po int, fieldplayoffs_a int, fieldplayoffs_e int, 
fieldplayoffs_dp int,fieldplayoffs_tp int, fieldplayoffs_pb int, fieldplayoffs_sb int, 
fieldplayoffs_cs int,  appearances_g_all int, appearances_gs int, 
appearances_g_batting int, appearances_defense int, 
appearances_g_p int, appearances_g_c int, appearances_g_1b int, 
appearances_g_2b int, appearances_g_3b int, appearances_g_ss int, 
appearances_g_lf int, appearances_g_cf int, appearances_g_rf int, appearances_dh int, 
appearances_ph int, appearances_pr int, yearMin float, yearMax float)

INSERT INTO dbo.Destination (Col1, Col2, Col3)
SELECT Col1, Col2, Col3
FROM dbo.Source